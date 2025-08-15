using Xunit;
using Chess.ViewModel.Game;
using Chess.Model.Game;
using Chess.Model.Piece;
using System.Linq;

namespace Chess.Tests
{
    public class ChessGameVMTests
    {
        [Fact]
        public void StandardBoard_HasCorrectPieceCount()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: false);
            Assert.Equal(32, vm.Board.Pieces.Count);
        }

        [Fact]
        public void Chess960Board_HasCorrectPieceCount()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: true);
            Assert.Equal(32, vm.Board.Pieces.Count);
        }

        [Fact]
        public void StandardBoard_KingsAreInCorrectPosition()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: false);
            var whiteKing = vm.Board.Pieces.FirstOrDefault(p => p.Piece is King && p.Piece.Color == Color.White);
            var blackKing = vm.Board.Pieces.FirstOrDefault(p => p.Piece is King && p.Piece.Color == Color.Black);
            Assert.NotNull(whiteKing);
            Assert.NotNull(blackKing);
            Assert.Equal(0, whiteKing.Position.Row);
            Assert.Equal(4, whiteKing.Position.Column);
            Assert.Equal(7, blackKing.Position.Row);
            Assert.Equal(4, blackKing.Position.Column);
        }

        [Fact]
        public void Chess960Board_KingsAreOnBackRank()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: true);
            var whiteKing = vm.Board.Pieces.FirstOrDefault(p => p.Piece is King && p.Piece.Color == Color.White);
            var blackKing = vm.Board.Pieces.FirstOrDefault(p => p.Piece is King && p.Piece.Color == Color.Black);
            Assert.NotNull(whiteKing);
            Assert.NotNull(blackKing);
            Assert.Equal(0, whiteKing.Position.Row);
            Assert.Equal(7, blackKing.Position.Row);

            var whiteRooks = vm.Board.Pieces
                .Where(p => p.Piece is Rook && p.Piece.Color == Color.White && p.Position.Row == 0)
                .OrderBy(p => p.Position.Column)
                .ToList();

            Assert.Equal(2, whiteRooks.Count);
            Assert.True(whiteRooks[0].Position.Column < whiteKing.Position.Column &&
                        whiteKing.Position.Column < whiteRooks[1].Position.Column);
        }

        [Fact]
        public void StandardBoard_WhiteQueenIsOnD1()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: false);
            var whiteQueen = vm.Board.Pieces.FirstOrDefault(p => p.Piece is Queen && p.Piece.Color == Color.White);
            Assert.NotNull(whiteQueen);
            Assert.Equal(0, whiteQueen.Position.Row);
            Assert.Equal(3, whiteQueen.Position.Column);
        }

        [Fact]
        public void Chess960Board_WhiteQueenIsOnBackRank()
        {
            var vm = new ChessGameVM(updates => updates[0], isChess960: true);
            var whiteQueen = vm.Board.Pieces.FirstOrDefault(p => p.Piece is Queen && p.Piece.Color == Color.White);
            Assert.NotNull(whiteQueen);
            Assert.Equal(0, whiteQueen.Position.Row);
            Assert.InRange(whiteQueen.Position.Column, 0, 7);
        }
    }
}