//-----------------------------------------------------------------------
// <copyright file="StandardRulebook.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
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
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Represents the standard chess rulebook.
    /// </summary>
    public class StandardRulebook : IRulebook
    {
        /// <summary>
        /// Represents the check rule of a standard chess game.
        /// </summary>
        private readonly CheckRule checkRule;

        /// <summary>
        /// Represents the end rule of a standard chess game.
        /// </summary>
        private readonly EndRule endRule;

        /// <summary>
        /// Represents the movement rule of a standard chess game.
        /// </summary>
        private readonly MovementRule movementRule;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardRulebook"/> class.
        /// </summary>
        public StandardRulebook()
        {
            var threatAnalyzer = new ThreatAnalyzer();
            var castlingRule = new CastlingRule(threatAnalyzer);
            var enPassantRule = new EnPassantRule();
            var promotionRule = new PromotionRule();

            this.checkRule = new CheckRule(threatAnalyzer);
            this.movementRule = new MovementRule(castlingRule, enPassantRule, promotionRule, threatAnalyzer);
            this.endRule = new EndRule(this.checkRule, this.movementRule);
        }

        /// <summary>
        /// Creates a new chess game according to the standard rulebook.
        /// </summary>
        /// <returns>The newly created chess game.</returns>
        public ChessGame CreateGame()
        {
            IEnumerable<PlacedPiece> makeBaseLine(int row, Color color)
            {
                yield return new PlacedPiece(new Position(row, 0), new Rook(color));
                yield return new PlacedPiece(new Position(row, 1), new Knight(color));
                yield return new PlacedPiece(new Position(row, 2), new Bishop(color));
                yield return new PlacedPiece(new Position(row, 3), new Queen(color));
                yield return new PlacedPiece(new Position(row, 4), new King(color));
                yield return new PlacedPiece(new Position(row, 5), new Bishop(color));
                yield return new PlacedPiece(new Position(row, 6), new Knight(color));
                yield return new PlacedPiece(new Position(row, 7), new Rook(color));
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
            var blackPlayer = new Player(Color.Black);
            var blackPieces = makePieces(6, 7, Color.Black);
            var board = new Board(whitePieces.AddRange(blackPieces));

            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        /// <summary>
        /// Creates a new chess 960 game
        /// </summary>
        /// <returns>The newly created chess 960 game.</returns>
        public ChessGame Create960Game()
        {
            IEnumerable<PlacedPiece> makeBaseLine(int row, Color color)
            {
                //yield return new PlacedPiece(new Position(row, 0), new Rook(color));

                //indices 0-7, 
                //0, 2, 4, 6 black
                //1, 3, 5, 7 white

                //1. Create a list of numbers 0-7 to represent open spaces
                //      When placing a piece, remove the corresponding number from the list

                List<int> spaces = Enumerable.Range(0, 8).ToList<int>();
                Random random = new Random();                                                       //rand, to choose spaces randomly

                //2. Place the king in a random place from 1-6, save it

                int kingLoc = random.Next(1, 7);
                //generate space 1-6
                spaces.Remove(kingLoc);                                                             //remove from list
                yield return new PlacedPiece(new Position(row, kingLoc), new King(color));          //place king, 7 spaces remain

                //3. Place two rooks, one on a random space left of the king, one to the right

                int leftRook = random.Next(0, kingLoc);                                             //get random space left of king
                int rightRook = random.Next(kingLoc + 1, 8);                                        //get random space right of king
                spaces.Remove(leftRook);                                                            //remove from list
                spaces.Remove(rightRook);
                yield return new PlacedPiece(new Position(row, leftRook), new Rook(color));         //place L rook, 6 remain
                yield return new PlacedPiece(new Position(row, rightRook), new Rook(color));        //place R rook, 5 remain

                //4. Place two bishops, one on an open even, the other on an open odd
                int Bish;

                while (true)                                                                        //Repeat until space is found
                {
                    Bish = spaces[random.Next(spaces.Count)];                                       //Get random space
                    if (Bish % 2 == 0)                                                              //If space is even...
                    {
                        spaces.Remove(Bish);                                                        //remove from list
                        yield return new PlacedPiece(new Position(row, Bish), new Bishop(color));   //place even bishop, 4 remain
                        break;                                                                      //end loop
                    }
                }
                while (true)                                                                        //Repeat until space is found
                {
                    Bish = spaces[random.Next(spaces.Count)];                                       //Get random space
                    if (Bish % 2 == 1)                                                              //If space is odd...
                    {
                        spaces.Remove(Bish);                                                        //remove from list
                        yield return new PlacedPiece(new Position(row, Bish), new Bishop(color));   //place odd bishop, 3 remain
                        break;                                                                      //end loop
                    }
                }
                //5. Place the knights on two random open spaces

                for (int i = 0; i < 2; i++)                                                         //Repeat the following twice:
                {
                    int Kni = spaces[random.Next(spaces.Count)];                                    //  Take random remaining space
                    spaces.Remove(Kni);                                                             //  Remove from list
                    yield return new PlacedPiece(new Position(row, Kni), new Knight(color));        //  Place a knight
                }                                                                                   //  Knights placed, 1 remain

                //6. Place the queen on the last remaining space
                yield return new PlacedPiece(new Position(row, spaces[0]), new Queen(color));       //  Place queen in remaining space

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
            var blackPlayer = new Player(Color.Black);
            var blackPieces = makePieces(6, 7, Color.Black);
            var board = new Board(whitePieces.AddRange(blackPieces));

            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        /// <summary>
        /// Gets the status of a chess game, according to the standard rulebook.
        /// </summary>
        /// <param name="game">The game state to be analyzed.</param>
        /// <returns>The current status of the game.</returns>
        public Status GetStatus(ChessGame game)
        {
            return this.endRule.GetStatus(game);
        }

        /// <summary>
        /// Gets all possible updates (i.e., future game states) for a chess piece on a specified position,
        /// according to the standard rulebook.
        /// </summary>
        /// <param name="game">The current game state.</param>
        /// <param name="position">The position to be analyzed.</param>
        /// <returns>A sequence of all possible updates for a chess piece on the specified position.</returns>
        public IEnumerable<Update> GetUpdates(ChessGame game, Position position)
        {
            var piece = game.Board.GetPiece(position, game.ActivePlayer.Color);
            var updates = piece.Map(
                p =>
                {
                    var moves = this.movementRule.GetCommands(game, p);
                    var turnEnds = moves.Select(c => new SequenceCommand(c, EndTurnCommand.Instance));
                    var records = turnEnds.Select
                    (
                        c => new SequenceCommand(c, new SetLastUpdateCommand(new Update(game, c)))
                    );
                    var futures = records.Select(c => c.Execute(game).Map(g => new Update(g, c)));
                    return futures.FilterMaybes().Where
                    (
                        e => !this.checkRule.Check(e.Game, e.Game.PassivePlayer)
                    );
                }
            );

            return updates.GetOrElse(Enumerable.Empty<Update>());
        }
    }
}