from mcts import Agent
from board import ChessBoard
import os
import copy
import time


chess_board = ChessBoard()

print("You will play the fist stone if typing in `1` and AI first if you type in `0`")
sente = int(input("Sente?  "))
if sente == 0:
    chess_board.play_stone((8, 8))
    chess_board.display_board()

agent = Agent(chess_board=copy.deepcopy(chess_board), max_searches=2500)

while not chess_board.is_ended():
    i = int(input('Abscissa: '))
    j = int(input('Ordinate: '))
    while not chess_board.is_legal((i, j)):
        print(f'Cannot play a stone at {(i, j)}. Try again.')
        i = int(input('Abscissa: '))
        j = int(input('Ordinate: '))

    os.system('cls')
    move = (i, j)
    chess_board.play_stone(move)
    chess_board.display_board()

    if chess_board.is_ended():
        break

    t0 = time.time()
    pre_visits, avg_visits = agent.update_root(move)
    print()
    if pre_visits != 0:
        print("-" * 40)
        print(f"Pre Visits: {pre_visits:.0f}")
        print(f"Avg Visits: {avg_visits:.0f}")
        print("-" * 40 + "\n")

    agent_loc, agent_win_rate = agent.search()
    chess_board.play_stone(agent_loc)
    chess_board.display_board()

    t1 = time.time()
    dt = t1 - t0
    print("\n"+"-"*40)
    print(f"AI play a stone at {agent_loc}")
    print(f"AI Win Rate: {agent_win_rate*100:.6f}%")
    print(f"  Time Cost: {dt:.6f}s")
    print("-"*40+"\n")
