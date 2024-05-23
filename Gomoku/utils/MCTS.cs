using System;
using System.Collections.Generic;

using System.Linq;

namespace Gomoku.utils
{
    internal class NodeSnapshot
    {
        public List<List<int>>? PreMoves { get; set; }
        public int Color {  get; set; }
        public List<int>? Move { get; set; }
        public int Visits { get; set; }
        public int Value { get; set; }
        public int Depth { get; set; }
        public bool BiggerExpanded { get; set; }
        public List<NodeSnapshot>? Children {  get; set; }

        public Node ConvertToNode(Node? parent)
        {
            var node = new Node(parent, new Board(PreMoves!.Select(move => (move[0], move[1])).ToList()), Color, (Move![0], Move![1]))
            {
                visits = Visits,
                value = Value,
                depth = Depth,
                biggerExpanded = BiggerExpanded,
            };
            node.children = Children!.Select(child => child.ConvertToNode(node)).ToList();

            return node;
        }
    }
    internal class Node
    {
        public Node? parent;
        public Board chessBoard;
        public int color;
        public (int, int) move;

        public int visits = 0;
        public int value = 0;
        public int depth = 1;
        public bool biggerExpanded = false;
        public List<Node> children = new();

        public Node(Node? parent, Board chessBoard, int color, (int, int) move)
        {
            this.parent = parent;
            this.chessBoard = chessBoard;
            this.color = color;
            this.move = move;
        }

        public void Expand()
        {
            var locs = LocSearch.KeyLocs(chessBoard, 1);
            foreach (var loc in locs)
            {
                var newChessBoard = new Board(chessBoard.moves);
                newChessBoard.PlayStone(loc);

                var child = new Node(this, newChessBoard, -newChessBoard.nowPlaying, loc);
                children.Add(child);
            }
        }

        public void BiggerExpand()
        {
            var locs = LocSearch.KeyLocs(chessBoard, 2);
            var childrenMove = children.Select(x => x.move);
            foreach (var loc in locs)
            {
                if (!childrenMove.Contains(loc))
                {
                    var newChessBoard = new Board(chessBoard.moves);
                    newChessBoard.PlayStone(loc);

                    var child = new Node(this, newChessBoard, -newChessBoard.nowPlaying, loc);
                    children.Add(child);
                }
            }
            biggerExpanded = true;
        }

        public override bool Equals(object? obj)
        {
            Node other = (Node)obj!;
            return move == other.move;
        }

        public override int GetHashCode()
        {
            return move.GetHashCode();
        }

        public NodeSnapshot ConvertToNodeSnapshot()
        {
            return new NodeSnapshot()
            {
                PreMoves = chessBoard.moves.Select(move => new List<int>() { move.Item1, move.Item2 }).ToList(),
                Color = color,
                Move = new() { move.Item1, move.Item2 },
                Visits = visits,
                Value = value,
                Depth = depth,
                BiggerExpanded = biggerExpanded,
                Children = children.Select(child => child.ConvertToNodeSnapshot()).ToList()
            };
        }
    }

    internal class Agent
    {
        public Node root;
        private Node currentNode;

        public bool isLastSearchUseManual;

        public Agent(Board chessBoard)
        {
            root = new(null, chessBoard, -chessBoard.nowPlaying, default);
            currentNode = root;
        }

        public void UpdateRoot((int, int) move)
        {
            if (root.children.Count != 0)
            {
                foreach (var child in root.children)
                {
                    if (child.move.Equals(move))
                    {
                        root = child;
                        root.parent = null;
                        return;
                    }
                }
            }

            var chessBoard = new Board(root.chessBoard.moves);
            chessBoard.PlayStone(move);
            root = new(null, chessBoard, -chessBoard.nowPlaying, move);
        }

        public void Search()
        {
            var searches = 1000;

            root.depth = 1;
            while (true)
            {
                if (root.children.Any(child => child.visits > searches))
                {
                    break;
                }

                currentNode = root;
                // Not a leaf node -> select the child node with the highest UCB
                while (currentNode.children.Count > 0)
                {
                    if (currentNode.depth <= 2 && !currentNode.biggerExpanded && root.chessBoard.moves.Count >= 8)
                    {
                        currentNode.BiggerExpand();
                    }

                    currentNode = currentNode.children.MaxBy(UCB)!;
                    currentNode.depth = currentNode.parent!.depth + 1;
                }

                // If the game has ended at the node -> back propagate
                if (currentNode.chessBoard.IsEnded())
                {
                    var value = 0;
                    if (currentNode.chessBoard.winner == currentNode.color)
                    {
                        value = 1;
                    }
                    else if (currentNode.chessBoard.winner == -currentNode.color)
                    {
                        value = -1;
                    }

                    BackPropagate(value);
                    continue;
                }

                // If the node has been visited or is the root -> expand and select the first child
                if (currentNode.visits != 0 || currentNode.parent == null)
                {
                    currentNode.Expand();
                    currentNode = currentNode.children[0];
                }

                int rolloutValue = Rollout();
                BackPropagate(rolloutValue);
            }
        }

        public Node SelectBestChild()
        {
            var bestChild = root.children.MaxBy(GetWinRate)!;
            root = bestChild;
            root.parent = null;

            return bestChild;
        }

        private static double UCB(Node node)
        {
            return GetWinRate(node) + Math.Pow(node.parent!.visits + 0.01, 0.25) / (node.visits + 0.01);
        }

        public static double GetWinRate(Node node)
        {
            return node.value / (2.0 * (node.visits + 0.01)) + 0.5;
        }

        private static (int, int) RandomChoose(List<(int, int)> locs, List<double> probs)
        {
            var random = new Random();
            var r = random.NextDouble();
            var cumulativeProb = 0.0;

            for (int i = 0; i < locs.Count; i++)
            {
                cumulativeProb += probs[i];

                if (r < cumulativeProb)
                {
                    return locs[i];
                }
            }

            return locs[^1];
        }

        private int Rollout()
        {
            var chessBoard = new Board(currentNode.chessBoard.moves);
            if (!chessBoard.IsEnded())
            {
                for (int i = 0; i < 20; i++)
                {
                    var (locs, probs) = LocSearch.KeyLocsProbs(chessBoard);
                    if (locs.Count == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        var loc = RandomChoose(locs, probs);
                        chessBoard.PlayStone(loc);
                    }

                    if (chessBoard.IsEnded())
                    {
                        break;
                    }
                }
            }

            if (chessBoard.winner == 0)
            {
                return 0;
            }
            else if (chessBoard.winner == currentNode.color)
            {
                return 1;
            }
            else if (chessBoard.winner == -currentNode.color)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        private void BackPropagate(int value)
        {
            while (currentNode.parent != null)
            {
                currentNode.visits += 1;
                currentNode.value += value;
                currentNode = currentNode.parent;
                value *= -1;
            }

            root.visits += 1;
        }
    }
}
