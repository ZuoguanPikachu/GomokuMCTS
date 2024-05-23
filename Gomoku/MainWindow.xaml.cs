using Gomoku.utils;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Gomoku
{
    public partial class MainWindow : Window
    {
        private bool gameStarted = false;
        private bool isPlayerRound = false;
        private Board chessBoard = new();
        private Agent agent;

        private readonly int cellSize = 50;
        private readonly int n = 15;
        private readonly int stoneSize = 40;
        private readonly int indicatorSize = 10;

        private Ellipse indicator = new();

        public MainWindow()
        {
            InitializeComponent();
            DrawChessboard();
            agent = new(chessBoard);
        }
        private void DrawChessboard()
        {
            chessBoardCanvas.Children.Clear();
            for (int i = 0; i < n; i++)
            {
                Line horizontalLine = new()
                {
                    X1 = cellSize,
                    Y1 = (i+1) * cellSize,
                    X2 = n * cellSize,
                    Y2 = (i+1) * cellSize,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    IsEnabled = false
                };

                Line verticalLine = new ()
                {
                    X1 = (i+1) * cellSize,
                    Y1 = cellSize,
                    X2 = (i+1) * cellSize,
                    Y2 = n * cellSize,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    IsEnabled = false
                };

                chessBoardCanvas.Children.Add(horizontalLine);
                chessBoardCanvas.Children.Add(verticalLine);
            }
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            SetStartState();

            if (AISente.IsChecked == true)
            {
                PlayStone((7, 7));
            }
            
            agent = new Agent(new Board(chessBoard.moves));
            SwitchToPlayerRound();
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            ResetBtn.IsEnabled = false;
            StartBtn.IsEnabled = true;
            isPlayerRound = false;
            
            StatusInfo.Text = string.Empty;
            LoadingLine.Visibility = Visibility.Collapsed;

            chessBoard = new Board();
            DrawChessboard();
        }

        private void PlayStone((int, int) move)
        {
            if (chessBoard.IsLegal(move))
            {
                if (chessBoard.nowPlaying == 1)
                {
                    Dispatcher.Invoke(() => DrawBlackStone(move));
                }
                else
                {
                    Dispatcher.Invoke(() => DrawWhiteStone(move));
                }
                Dispatcher.Invoke(() => DrawIndicator(move));
                chessBoard.PlayStone(move);
            }
        }

        private void DrawBlackStone((int, int) move)
        {
            Ellipse stone = new()
            {
                Width = stoneSize,
                Height = stoneSize,
                Fill = BlackStoneBrush()
            };
            Canvas.SetLeft(stone, (move.Item1 + 1) * cellSize - stoneSize / 2);
            Canvas.SetTop(stone, (move.Item2 + 1) * cellSize - stoneSize / 2);
            chessBoardCanvas.Children.Add(stone);
        }

        private void DrawWhiteStone((int, int) move)
        {
            Ellipse stone = new()
            {
                Width = stoneSize,
                Height = stoneSize,
                Fill = WhiteStoneBrush()
            };
            Canvas.SetLeft(stone, (move.Item1 + 1) * cellSize - stoneSize / 2);
            Canvas.SetTop(stone, (move.Item2 + 1) * cellSize - stoneSize / 2);
            chessBoardCanvas.Children.Add(stone);
        }

        private void DrawIndicator((int, int) move)
        {
            chessBoardCanvas.Children.Remove(indicator);

            indicator = new()
            {
                Width = indicatorSize,
                Height = indicatorSize,
                Fill = new SolidColorBrush() { Color = Colors.Blue },
            };
            Canvas.SetLeft(indicator, (move.Item1 + 1) * cellSize - indicatorSize / 2);
            Canvas.SetTop(indicator, (move.Item2 + 1) * cellSize - indicatorSize / 2);
            chessBoardCanvas.Children.Add(indicator);
        }

        private void PlayerDrop(object sender, MouseButtonEventArgs e)
        {
            if (gameStarted && isPlayerRound && !chessBoard.IsEnded())
            {
                Point position = e.GetPosition(chessBoardCanvas);
                if (position.X >= 25 && position.Y >= 25 && position.X <= 775 && position.Y <= 775) 
                {
                    int i = ((int)(position.X - 25)) / 50;
                    int j = ((int)(position.Y - 25)) / 50;

                    if (chessBoard.IsLegal((i, j)))
                    {
                        OneRound((i, j));
                    }
                }
            }
        }

        private async void OneRound((int, int) move)
        {
            PlayStone(move);
            SwitchToAIRound();

            if (chessBoard.IsEnded())
            {
                ShowGameEnded();
            }
            else
            {
                await Task.Run(() =>
                {
                    ShowSearching();
                    DisableResetBtn();
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    agent.UpdateRoot(move);
                    agent.Search();

/*                    if (stopwatch.ElapsedMilliseconds > 5 * 1000)
                    {
                        ManualManager.SaveManual(agent.root);
                    }
                    else if (agent.isLastSearchUseManual)
                    {
                        ManualManager.SaveManual(agent.root);
                    }*/
                    
                    var bestChild = agent.SelectBestChild();
                    PlayStone(bestChild.move);

                    stopwatch.Stop();
                    ShowStatusInfo(bestChild, stopwatch.ElapsedMilliseconds);
                    EnableResetBtn();
                    SwitchToPlayerRound();
                });

                if (chessBoard.IsEnded())
                {
                    ShowGameEnded();
                }
            }
        }

        private void SwitchToAIRound()
        {
            isPlayerRound = false;
        }

        private void SwitchToPlayerRound()
        {
            isPlayerRound = true;
        }

        private void DisableResetBtn()
        {
            Dispatcher.Invoke(() => { ResetBtn.IsEnabled = false; });
        }

        private void EnableResetBtn()
        {
            Dispatcher.Invoke(() => { ResetBtn.IsEnabled = true; });
        }

        private void ShowSearching()
        {
            Dispatcher.Invoke(() => { LoadingLine.Visibility = Visibility.Visible; });
        }

        private void ShowStatusInfo(Node currentNode, long elapsedMilliseconds) 
        {
            Dispatcher.Invoke(() => {
                LoadingLine.Visibility = Visibility.Collapsed;
                double winRate = Agent.GetWinRate(currentNode)*100;
                double dt = elapsedMilliseconds / 1000.0;
                int steps = currentNode.visits;
                StatusInfo.Text = $"{winRate:F1}%; {dt:F1}s; {steps}";
            });
        }

        private static void ShowGameEnded()
        {
            MessageBox.Show("游戏结束！！！");
        }

        private void SetStartState()
        {
            gameStarted = true;
            ResetBtn.IsEnabled = true;
            StartBtn.IsEnabled = false;
        }

        private static Brush BlackStoneBrush()
        {
            return new RadialGradientBrush()
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.White, 0.0),
                    new GradientStop(Colors.LightGray, 0.2),
                    new GradientStop(Colors.Black, 1.0)
                },
                Center = new Point(0.3, 0.3),
                GradientOrigin = new Point(0.3, 0.3),
                RadiusX = 0.4,
                RadiusY = 0.4
            };
        }

        private static Brush WhiteStoneBrush()
        {
            return new RadialGradientBrush()
            {
                GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Colors.White, 0.0),
                            new GradientStop(Colors.WhiteSmoke, 0.2),
                            new GradientStop(Colors.LightGray, 1.0)
                        },
                Center = new Point(0.3, 0.3),
                GradientOrigin = new Point(0.3, 0.3),
                RadiusX = 0.4,
                RadiusY = 0.4
            };
        }
    }
}
