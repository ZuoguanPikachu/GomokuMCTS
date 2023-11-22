from mcts import Agent
from board import ChessBoard
import os
import copy
import time


chess_board = ChessBoard()
agent = Agent(chess_board=copy.deepcopy(chess_board), max_searches=10000)

while not chess_board.is_ended():
    i = int(input('Abscissa: '))
    j = int(input('Ordinate: '))
    os.system('cls')
    move = (i, j)
    chess_board.play_stone(move)
    chess_board.display_board()

    t0 = time.time()
    agent.update_root(move)
    agent_loc, agent_win_rate = agent.search()
    chess_board.play_stone(agent_loc)
    chess_board.display_board()

    t1 = time.time()
    dt = t1 - t0
    print("\n"+"-"*40)
    print(f"time cost: {dt:.6f}s")
    print(f"ai win rate: {agent_win_rate*100:.6f}%")
    print("-"*40+"\n")
