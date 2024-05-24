using System.Collections.Generic;
using System;

namespace Gomoku.utils
{
    internal class Board
    {
        private const int winLength = 5;
        public readonly int size = 15;
        public int[,] board = new int[15, 15];
        public List<(int, int)> moves = [];
        public int nowPlaying = 1;
        public int winner = 0;

        public Board() { }

        public Board(List<(int, int)> moves)
        {
            foreach (var move in moves)
            {
                PlayStone(move);
            }
        }

        public bool IsLegal((int, int) move)
        {
            return board[move.Item1, move.Item2] == 0;
        }

        public void PlayStone((int, int) move)
        {
            board[move.Item1, move.Item2] = nowPlaying;
            moves.Add(move);
            nowPlaying = -nowPlaying;
        }

        public bool IsEnded()
        {
            if (moves.Count == 0)
            {
                return false;
            }
            var locI = moves[^1].Item1;
            var locJ = moves[^1].Item2;
            var color = -nowPlaying;
            int[] sgnI = [1, 0, 1, 1];
            int[] sgnJ = [0, 1, 1, -1];
            for (int iter = 0; iter < 4; iter++)
            {
                var length = 0;
                var prm1 = sgnI[iter] == 1 ? locI : locJ;
                var prm2 = sgnJ[iter] == 1 ? locJ : sgnJ[iter] == 0 ? locI : size - 1 - locJ;
                var startBias = -Math.Min(prm1, prm2) < winLength - 1 ? -Math.Min(prm1, prm2) : -winLength + 1;
                var endBias = Math.Max(prm1, prm2) > size - winLength ? size - 1 - Math.Max(prm1, prm2) : winLength - 1;
                for (int k = startBias; k <= endBias; k++)
                {
                    int stone = board[locI + k * sgnI[iter], locJ + k * sgnJ[iter]];
                    if (color > 0 && stone > 0 || color < 0 && stone < 0)
                    {
                        length++;
                    }
                    else
                    {
                        length = 0;
                    }
                    if (length == winLength)
                    {
                        winner = color > 0 ? 1 : -1;
                        return true;
                    }
                }
            }
            if (moves.Count == 225)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
