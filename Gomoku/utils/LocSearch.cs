using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gomoku.utils
{
    class LocInfo
    {
        public (int, int) loc;
        public List<(double, string)> values = [];
        public double selfValue;
        public double opponentValue;

        public void CalcValue()
        {
            var selfK = 1.0;
            var opponentK = 1.0;

            foreach (var (v, s) in values.OrderByDescending(item => item.Item1))
            {
                if (s == "self")
                {
                    selfValue += v * selfK;
                    selfK *= 0.1;
                }
                else
                {
                    opponentValue += v * opponentK;
                    opponentK *= 0.1;
                }
            }
        }
    }

    static class LocSearch
    {
        public static HashSet<(int, int)> Vacancies(Board board, int bias)
        {
            var vacancies = new HashSet<(int, int)>();
            foreach ((int move_i, int move_j) in board.moves)
            {
                for (int i = -bias; i <= bias; i++)
                {
                    if (move_i - i < 0 || move_i - i >= board.size)
                    {
                        continue;
                    }
                    for (int j = -bias; j <= bias; j++)
                    {
                        if (move_j - j < 0 || move_j - j >= board.size)
                        {
                            continue;
                        }
                        vacancies.Add((move_i - i, move_j - j));
                    }
                }
            }
            var occupied = new HashSet<(int, int)>(board.moves);
            vacancies.ExceptWith(occupied);
            return vacancies;
        }

        public static IEnumerable<LocInfo> KeyLocsInfo(Board board, int bias)
        {
            var vacancies = Vacancies(board, bias);
            int[][] directions = [[0, 1], [1, 0], [1, 1], [1, -1]];
            var locs = new List<LocInfo>();

            foreach ((int row, int col) in vacancies)
            {
                var loc = new LocInfo() { loc = (row, col) };
                foreach (int[] direction in directions)
                {
                    int dr = direction[0];
                    int dc = direction[1];

                    var fragment = new List<int>(capacity: 9);
                    for (int i = -4; i <= 4; i++)
                    {
                        var r = row + i * dr;
                        var c = col + i * dc;
                        if (0 <= r && r < board.size && 0 <= c && c < board.size)
                        {
                            fragment.Add(board.board[r, c]);
                        }
                    }

                    if (fragment.Count < 5)
                    {
                        continue;
                    }

                    var (value, valueSource) = FragmentEvaluate(fragment, board.nowPlaying);

                    if (value > 0)
                    {
                        loc.values.Add((value, valueSource));
                    }
                }

                if (loc.values.Count >= 1)
                {
                    loc.CalcValue();
                    locs.Add(loc);
                }
            }

            if (locs.Count == 0)
            {
                return [new LocInfo() { loc=vacancies.First(), opponentValue=0, selfValue=0 }];
            }

            var opponentMax = locs.Max(item => item.opponentValue);

            if (opponentMax >= 4)
            {
                return locs.Where(item => item.selfValue >= 4 || item.opponentValue >= 4);
            }
            else if (opponentMax >= 3.25)
            {
                return locs.Where(item => item.selfValue >= 3.25 || item.opponentValue >= 3.25);
            }
            else if (locs.Any(item => item.opponentValue >= 2.75 && item.opponentValue < 3))
            {
                return locs.Where(item => item.selfValue >= 2.5 || item.opponentValue >= 2.75);
            }

            else if(locs.Any(item => item.selfValue >= 3.25))
            {
                return locs.Where(item => item.selfValue >= 3.25);
            }
            else if (locs.Any(item => item.selfValue >= 2.75 && item.selfValue < 3))
            {
                return locs.Where(item => item.selfValue >= 2.75);
            }
            else
            {
                return locs.OrderByDescending(item => item.selfValue + item.opponentValue).Take(10);
            }
        }

        public static List<(int, int)> KeyLocs(Board board, int bias)
        {
            var locsInfo = KeyLocsInfo(board, bias);

            return locsInfo.Select(item => item.loc).ToList();
        }

        public static (int, int) RandomMove(Board board)
        {
            var locsInfo = KeyLocsInfo(board, 1);

            var expSum = 0.0;
            var weigthts = new List<double>();
            foreach (var item in locsInfo)
            {
                var v = item.selfValue + item.opponentValue;
                weigthts.Add(v);
                expSum += Math.Exp(v);
            }

            var random = new Random();
            var r = random.NextDouble();
            var cumulativeProb = 0.0;
            for (int i = 0; i < weigthts.Count; i++)
            {
                cumulativeProb += weigthts[i] / expSum;

                if (r < cumulativeProb)
                {
                    return locsInfo.ElementAt(i).loc;
                }
            }

            return locsInfo.Last().loc;
        }

        public static (double, string) FragmentEvaluate(List<int> fragment, int currentStone)
        {
            var maxValue = double.MinValue;
            var maxValueSource = string.Empty;

            var window = new Queue<int>();
            int selfCount = 0;
            int opponentCount = 0;

            for (int i = 0; i < fragment.Count; i++)
            {
                var stone = fragment[i];
                window.Enqueue(stone);

                if (stone == currentStone)
                {
                    selfCount++;
                }
                else if (stone == -currentStone)
                {
                    opponentCount++;
                }

                if (window.Count == 5)
                {
                    var v = double.MinValue;
                    var source = string.Empty;
                    if (opponentCount == 0)
                    {
                        v = selfCount;
                        source = "self";
                    }
                    else if (selfCount == 0)
                    {
                        v = opponentCount;
                        source = "opponent";
                    }

                    if (v > 1 && v == maxValue && source == maxValueSource)
                    {
                        v += 0.5;
                    }

                    if (v > maxValue)
                    {
                        maxValue = v;
                        maxValueSource = source;
                    }

                    stone = window.Dequeue();
                    if (stone == currentStone)
                    {
                        selfCount--;
                    }
                    else if (stone == -currentStone)
                    {
                        opponentCount--;
                    }
                }
            }

            return (maxValue, maxValueSource);
        }
    }
}
