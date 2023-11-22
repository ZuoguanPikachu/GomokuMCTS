import random
import math
import copy


class Node:
    def __init__(self, parent=None, chess_board=None, color=0):
        self.parent = parent
        self.chess_board = chess_board
        self.color = color
        self.visits = 0
        self.value = 0
        self.children = []


def get_win_rate(node):
    return (node.value / (2*node.visits)) + 0.5


class Agent:
    def __init__(self, chess_board, max_searches):
        self.root = Node(chess_board=chess_board, color=-chess_board.now_playing)
        self.current_node = self.root
        self.max_searches = max_searches

    def update_root(self, move):
        if self.root.children != []:
            for child in self.root.children:
                if child.chess_board.moves[-1] == move:
                    children_count = len(self.root.children)
                    parent_visits = self.root.visits

                    self.root = child
                    self.root.parent = None

                    return self.root.visits, (parent_visits/children_count)

        chess_board = copy.deepcopy(self.root.chess_board)
        chess_board.play_stone(move)
        self.root = Node(chess_board=chess_board, color=-chess_board.now_playing)
        return 0, 0

    def expand(self):
        vacancies = self.current_node.chess_board.adjacent_vacancies()
        for move in vacancies:
            chess_board = copy.deepcopy(self.current_node.chess_board)
            chess_board.play_stone(move)
            child = Node(
                parent=self.current_node,
                chess_board=chess_board,
                color=-chess_board.now_playing
            )
            self.current_node.children.append(child)

    def roll_out(self):
        chess_board = copy.deepcopy(self.current_node.chess_board)
        while not chess_board.is_ended():
            vacancies = chess_board.adjacent_vacancies()
            loc = random.choice(list(vacancies))
            chess_board.play_stone(loc)
        if chess_board.winner == self.current_node.color:
            return 1
        elif chess_board.winner == -self.current_node.color:
            return -1
        else:
            return 0

    def back_propagate(self, value):
        while self.current_node.parent is not None:
            self.current_node.visits += 1
            self.current_node.value += value
            self.current_node = self.current_node.parent
            value *= -1

        self.root.visits += 1

    def search(self):
        for _ in range(self.max_searches):
            self.current_node = self.root

            # 不是叶节点->选择ucb最大的子节点
            while self.current_node.children != []:
                # 随机选择未访问节点
                zero_visits = []
                for child in self.current_node.children:
                    if child.visits == 0:
                        zero_visits.append(child)
                if zero_visits != []:
                    self.current_node = random.choice(zero_visits)

                # 选择ucb最大的子节点
                else:
                    ucb = lambda node: (node.value / node.visits) + (2 * (math.log(node.parent.visits) / node.visits) ** 0.5)
                    # ucb = lambda node: get_win_rate(node) + (2 * (math.log(node.parent.visits) / node.visits) ** 0.5)
                    ucb_list = [ucb(child) for child in self.current_node.children]
                    idx_max = ucb_list.index(max(ucb_list))
                    self.current_node = self.current_node.children[idx_max]

            if self.current_node.chess_board.is_ended():
                if self.current_node.chess_board.winner == self.current_node.color:
                    value = 1
                elif self.current_node.chess_board.winner == -self.current_node.color:
                    value = -1
                else:
                    value = 0

                self.back_propagate(value)
                continue

            if (self.current_node.visits != 0) or (self.current_node.parent is None):
                self.expand()
                self.current_node = self.current_node.children[0]

            value = self.roll_out()
            self.back_propagate(value)

        best_child = max(self.root.children, key=lambda child: get_win_rate(child))
        agent_win_rate = get_win_rate(best_child)
        loc = best_child.chess_board.moves[-1]

        self.root = best_child
        self.root.parent = None
        return loc, agent_win_rate
