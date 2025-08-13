namespace Chess.Model.Rule
{
    using Chess.Model.Command;
    using Chess.Model.Data;
    using Chess.Model.Game;
    using Chess.Model.Piece;
    using Chess.Model.Visitor;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Represents a Chess960 (Fischer Random) rulebook.
    /// </summary>
    public class Chess960Rulebook : IRulebook
    {
        private readonly CheckRule checkRule;
        private readonly EndRule endRule;
        private readonly MovementRule movementRule;
        private readonly Random rng;

        public Chess960Rulebook(int? seed = null)
        {
            var threatAnalyzer = new ThreatAnalyzer();
            var castlingRule = new CastlingRule(threatAnalyzer);
            var enPassantRule = new EnPassantRule();
            var promotionRule = new PromotionRule();

            this.checkRule = new CheckRule(threatAnalyzer);
            this.movementRule = new MovementRule(castlingRule, enPassantRule, promotionRule, threatAnalyzer);
            this.endRule = new EndRule(this.checkRule, this.movementRule);

            this.rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Creates a new chess game according to the Chess960 rulebook.
        /// </summary>
        public ChessGame CreateGame()
        {
            var backRank = GenerateBackRank();

            IEnumerable<PlacedPiece> makeBaseLine(int row, Color color)
            {
                for (int file = 0; file < 8; file++)
                {
                    ChessPiece piece = backRank[file] switch
                    {
                        PieceKind.Rook => new Rook(color),
                        PieceKind.Knight => new Knight(color),
                        PieceKind.Bishop => new Bishop(color),
                        PieceKind.Queen => new Queen(color),
                        PieceKind.King => new King(color),
                        _ => throw new InvalidOperationException("Unsupported piece kind.")
                    };

                    yield return new PlacedPiece(new Position(row, file), piece);
                }
            }

            IEnumerable<PlacedPiece> makePawns(int row, Color color) =>
                Enumerable.Range(0, 8).Select(
                    i => new PlacedPiece(new Position(row, i), new Pawn(color))
                );

            IImmutableDictionary<Position, ChessPiece> makePieces(int pawnRow, int baseRow, Color color)
            {
                var pawns = makePawns(pawnRow, color);
                var baseLine = makeBaseLine(baseRow, color);
                var pieces = baseLine.Union(pawns);
                var empty = ImmutableSortedDictionary.Create<Position, ChessPiece>(PositionComparer.DefaultComparer);
                return pieces.Aggregate(empty, (s, p) => s.Add(p.Position, p.Piece));
            }

            var whitePlayer = new Player(Color.White);
            var whitePieces = makePieces(1, 0, Color.White);

            // Mirror white back rank exactly for black.
            var blackPlayer = new Player(Color.Black);
            var blackPieces = makePieces(6, 7, Color.Black);

            var board = new Board(whitePieces.AddRange(blackPieces));
            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        public Status GetStatus(ChessGame game) => this.endRule.GetStatus(game);

        public IEnumerable<Update> GetUpdates(ChessGame game, Position position)
        {
            var piece = game.Board.GetPiece(position, game.ActivePlayer.Color);
            var updates = piece.Map(
                p =>
                {
                    var moves = this.movementRule.GetCommands(game, p);
                    var turnEnds = moves.Select(c => new SequenceCommand(c, EndTurnCommand.Instance));
                    var records = turnEnds.Select(c => new SequenceCommand(c, new SetLastUpdateCommand(new Update(game, c))));
                    var futures = records.Select(c => c.Execute(game).Map(g => new Update(g, c)));
                    return futures.FilterMaybes().Where(e => !this.checkRule.Check(e.Game, e.Game.PassivePlayer));
                }
            );

            return updates.GetOrElse(Enumerable.Empty<Update>());
        }

        private enum PieceKind { Rook, Knight, Bishop, Queen, King }

        /// <summary>
        /// Creates a valid Chess960 back rank:
        /// - Two bishops on opposite-color squares.
        /// - King between the two rooks.
        /// - Remaining pieces are one queen and two knights.
        /// Returns an array of length 8: index=file (0..7), value=PieceKind.
        /// </summary>
        private PieceKind[] GenerateBackRank()
        {
            var files = Enumerable.Range(0, 8).ToList();
            var layout = new PieceKind[8];

            // 1) Place bishops on opposite colors.
            // Light squares are even files (0-based) on rank 0; dark squares are odd files.
            var lightFiles = files.Where(f => f % 2 == 0).ToList();
            var darkFiles = files.Where(f => f % 2 == 1).ToList();

            int bLight = TakeRandom(lightFiles);
            int bDark = TakeRandom(darkFiles);

            layout[bLight] = PieceKind.Bishop;
            layout[bDark] = PieceKind.Bishop;
            files.Remove(bLight);
            files.Remove(bDark);

            // 2) Place the queen on a random remaining file.
            int q = TakeRandom(files);
            layout[q] = PieceKind.Queen;
            files.Remove(q);

            // 3) Place two knights on two random remaining files.
            int n1 = TakeRandom(files);
            files.Remove(n1);
            int n2 = TakeRandom(files);
            files.Remove(n2);

            layout[n1] = PieceKind.Knight;
            layout[n2] = PieceKind.Knight;

            // 4) Three files left → must be R, K, R with K between the two rooks.
            files.Sort(); // e0 < e1 < e2
            int e0 = files[0], e1 = files[1], e2 = files[2];

            layout[e0] = PieceKind.Rook;
            layout[e1] = PieceKind.King;  // king is between rooks
            layout[e2] = PieceKind.Rook;

            return layout;
        }

        /// <summary>
        /// Takes a random element from the given list and returns it.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private int TakeRandom(List<int> list)
        {
            int idx = this.rng.Next(list.Count);
            return list[idx];
        }
    }
}