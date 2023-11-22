import warnings


class ChessBoard:
    def __init__(self, size=15, win_len=5):
        self.size = size
        self.win_len = win_len
        self.board = [[0 for _ in range(size)] for _ in range(size)]
        self.moves = []
        self.now_playing = 1
        self.winner = 0

    def is_legal(self, move):
        i, j = move
        is_inside = (i >= 0) and (i < self.size) and (j >= 0) and (j < self.size)
        is_vacancy = self.board[i][j] == 0
        return is_inside and is_vacancy

    def play_stone(self, move):
        if not self.is_legal(move):
            warnings.warn(f'Cannot play a stone at {move}.', Warning, 3)
        else:
            self.board[move[0]][move[1]] = self.now_playing
            self.moves.append(move)
            self.now_playing = -self.now_playing

    def display_board(self):
        if not self.moves:
            return
        else:
            i_ticks = '  0 1 2 3 4 5 6 7 8 9 A B C D E'
            i_ticks = i_ticks[0:1+2*self.size]
            print(i_ticks)
            for j in range(self.size):
                if j < 10:
                    print(j, end='')
                else:
                    print(chr(55 + j), end='')
                for i in range(self.size):
                    print(' ', end='')
                    if self.board[i][j] > 0:
                        print('o', end='')
                    elif self.board[i][j] < 0:
                        print('x', end='')
                    else:
                        print(' ', end='')
                    if i == self.size - 1:
                        print()

    def adjacent_vacancies(self):
        vacancies = set()
        if self.moves:
            bias = range(-1, 2)
            for move in self.moves:
                for i in bias:
                    if (move[0]-i < 0) or (move[0]-i >= self.size):
                        continue
                    for j in bias:
                        if (move[1]-j < 0) or (move[1]-j >= self.size):
                            continue
                        vacancies.add((move[0]-i, move[1]-j))
            occupied = set(self.moves)
            vacancies -= occupied
        return vacancies

    def is_ended(self) -> bool:
        if not self.moves:
            return False
        loc_i, loc_j = self.moves[-1]
        color = -self.now_playing
        sgn_i = [1, 0, 1, 1]
        sgn_j = [0, 1, 1, -1]
        for iter_ in range(4):
            length = 0
            prm1 = loc_i if sgn_i[iter_] == 1 else loc_j
            prm2 = loc_j if sgn_j[iter_] == 1 else (loc_i if sgn_j[iter_] == 0 else self.size - 1 - loc_j)
            start_bias = -min(prm1, prm2) if min(prm1, prm2) < self.win_len-1 else -self.win_len+1
            end_bias = self.size - 1 - max(prm1, prm2) if max(prm1, prm2) > self.size-self.win_len else self.win_len-1
            for k in range(start_bias, end_bias+1):
                stone = self.board[loc_i + k * sgn_i[iter_]][loc_j + k * sgn_j[iter_]]
                if color > 0 and stone > 0 or color < 0 and stone < 0:
                    length += 1
                else:
                    length = 0
                if length == self.win_len:
                    self.winner = 1 if color > 0 else -1
                    return True
        if len(self.moves) == self.size ** 2:
            return True
        else:
            return False
