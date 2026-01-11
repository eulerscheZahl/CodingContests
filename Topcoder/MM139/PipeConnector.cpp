#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <iostream>
#include <queue>
#include <sstream>
#include <stack>
#include <unordered_set>
#include <vector>

using namespace std;

chrono::_V2::system_clock::time_point start;

class Path;
class Cell {
   public:
    int X, Y, ID;
    int Color;
    int Value;
    vector<Cell*> Neighbors;
    vector<Path*> Paths;

    void Fill(int x, int y, int value, int color);
    void InitNeighbors();
    void BFS(int crossings = 0);
    string Print();
    string ToString();
    Path* BuildPath(Cell* toConnect, int dist[], int dir = -1);
};

class Path {
   public:
    vector<Cell*> Cells;
    int Color();
    Cell* Start();
    Cell* End();
    int Score();
    string Print();
    int Remove();
    void Apply();
};

class BfsState {
   public:
    Cell* _Cell;
    int Crossings;
};

int BoardSize;
int BoardArea;
int BoardColors;
int BoardPenalty;
Cell BoardGrid[30][30];
Path paths[30 * 30 * 15];
stack<Path*> freePaths;
int dx[] = {0, 1, 0, -1};
int dy[] = {1, 0, -1, 0};
int dist[30 * 30 * 10];
double scores[900];

int bfsCount;

int random(int max) {
    if (max == 0) return 0;
    return rand() % max;
}

int random(int min, int max) { return min + random(max - min); }

double randomDouble() { return rand() / RAND_MAX; }

void Cell::Fill(int x, int y, int value, int color) {
    X = x;
    Y = y;
    ID = X + BoardSize * Y;
    Color = color;
    Value = value;
}

void Cell::InitNeighbors() {
    for (int dir = 0; dir < 4; dir++) {
        int x = X + dx[dir];
        int y = Y + dy[dir];
        if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize) Neighbors.push_back(&BoardGrid[x][y]);
    }
}

void Cell::BFS(int crossings) {
    bfsCount++;
    memset(dist, 0, BoardArea * (crossings + 1) * sizeof(int));
    queue<BfsState> queue;
    BfsState start;
    start._Cell = this;
    start.Crossings = 0;
    queue.push(start);
    while (queue.size()) {
        BfsState q = queue.front();
        queue.pop();
        if (q._Cell != this && q._Cell->Value > 0) continue;
        for (Cell* n : q._Cell->Neighbors) {
            int c = q.Crossings + n->Paths.size();
            if (c > crossings || dist[n->ID + BoardArea * c] > 0) continue;
            dist[n->ID + BoardArea * c] = 1 + dist[q._Cell->ID + BoardArea * q.Crossings];
            for (int i = c + 1; i <= crossings; i++) {
                if (dist[n->ID + BoardArea * i] == 0) dist[n->ID + BoardArea * i] = dist[n->ID + BoardArea * c];
            }
            BfsState state;
            state._Cell = n;
            state.Crossings = c;
            queue.push(state);
        }
    }
    dist[this->ID + BoardArea * 0] = 0;
}

string Cell::Print() { return to_string(Y) + " " + to_string(X); }

string Cell::ToString() { return to_string(X) + " " + to_string(Y); }

Path* Cell::BuildPath(Cell* toConnect, int dist[], int dir) {
    Path* path = freePaths.top();
    freePaths.pop();
    path->Cells.clear();
    int c = 0;
    while (dist[toConnect->ID + BoardArea * c] == 0) c++;
    while (dist[toConnect->ID + BoardArea * c] > 1) {
        path->Cells.push_back(toConnect);
        int newC = c - toConnect->Paths.size();
        int off = dir == -1 ? random(toConnect->Neighbors.size()) : dir;
        for (int i = 0; i < toConnect->Neighbors.size(); i++) {
            Cell* next = toConnect->Neighbors[(i + off) % toConnect->Neighbors.size()];
            if (next->Value == 0 && dist[next->ID + BoardArea * newC] == dist[toConnect->ID + BoardArea * c] - 1) {
                toConnect = next;
                break;
            }
        }
        c = newC;
    }
    path->Cells.push_back(toConnect);
    path->Cells.push_back(this);

    path->Apply();
    return path;
}

int Path::Color() { return Cells[0]->Color; }

Cell* Path::Start() { return Cells[0]; }
Cell* Path::End() { return Cells[Cells.size() - 1]; }
int Path::Score() {
    int result = Start()->Value * End()->Value;
    for (Cell* cell : Cells) result -= BoardPenalty * (cell->Paths.size() - 1);
    return result;
}

string Path::Print() {
    stringstream ss;
    ss << Cells.size();
    for (Cell* cell : Cells) ss << endl << cell->Print();
    return ss.str();
}

int Path::Remove() {
    int result = Start()->Value * End()->Value;
    for (Cell* cell : Cells) {
        cell->Paths.erase(std::remove(cell->Paths.begin(), cell->Paths.end(), this), cell->Paths.end());
        result -= BoardPenalty * cell->Paths.size();
    }
    return result;
}

void Path::Apply() {
    for (Cell* cell : Cells) cell->Paths.push_back(this);
}

class Board {
   public:
    void ReadInput() {
        cin >> BoardSize >> BoardColors >> BoardPenalty;
        BoardArea = BoardSize * BoardSize;

        for (int y = 0; y < BoardSize; y++) {
            for (int x = 0; x < BoardSize; x++) {
                int value, color;
                cin >> value >> color;
                BoardGrid[x][y].Fill(x, y, value, color);
            }
        }

        for (int y = 0; y < BoardSize; y++) {
            for (int x = 0; x < BoardSize; x++) {
                BoardGrid[x][y].InitNeighbors();
            }
        }
    }

    void GreedyInit(vector<Cell*>& withValue, vector<Path*>& result) {
        auto comp = [&](Cell* a, Cell* b) -> bool { return a->Value > b->Value; };
        sort(withValue.begin(), withValue.end(), comp);
        for (Cell* cell : withValue) {
            if (cell->Paths.size() > 0) continue;
            cell->BFS();
            vector<Cell*> partners;
            for (Cell* c : withValue) {
                if (c->Value == cell->Value && c->Color == cell->Color && dist[c->ID] > 0) partners.push_back(c);
            }
            if (partners.size() == 0) continue;

            auto compDist = [&](Cell* a, Cell* b) -> bool { return a->Value / sqrt(dist[a->ID]) > b->Value / sqrt(dist[b->ID]); };
            sort(partners.begin(), partners.end(), compDist);
            Cell* toConnect = partners[0];
            result.push_back(cell->BuildPath(toConnect, dist));
        }
    }

    void Mutate(vector<Path*>& result, vector<Cell*>& withValue, int startTime, int runMaxTime, int totalMaxTime) {
        int totalScore = 0;
        for (Path* path : result) totalScore += path->Score();
        int randomRange = 2;
        int toUndo = 8;
        vector<Cell*> empty;
        for (Cell* c : withValue) {
            if (c->Paths.size() == 0) empty.push_back(c);
        }
        int runs = 0;
        int time = 0;
		int lastImprovement = 0;
        while (true) {
            if (++runs % 128 == 0) time = (int)chrono::duration_cast<chrono::milliseconds>(chrono::high_resolution_clock::now() - start).count() - startTime;
            if (runs - lastImprovement > 800 || time > totalMaxTime) break;
            int crossings = 30 * time / (max(4, BoardPenalty) * runMaxTime);
            // undo some paths near each other
            vector<Path*> undone;
            int x = random(BoardSize);
            int y = random(BoardSize);
            int oldScore = 0;
            int maxColors = random(2, 1 + BoardColors);
            int attempts = 0;
            unordered_set<int> usedColors;
            while (undone.size() < toUndo && undone.size() < result.size()) {
                attempts++;
                if (BoardGrid[x][y].Paths.size() > 0) {
                    Path* toRemove = BoardGrid[x][y].Paths[random(BoardGrid[x][y].Paths.size())];
                    if (usedColors.size() < maxColors || attempts > 1000 || usedColors.find(toRemove->Color()) != usedColors.end()) {
                        oldScore += toRemove->Remove();
                        undone.push_back(toRemove);
                        usedColors.emplace(toRemove->Color());
                        int r = random(5);
                        if (r == 0) {
                            x = toRemove->Start()->X;
                            y = toRemove->Start()->Y;
                        }
                        if (r == 1) {
                            x = toRemove->End()->X;
                            y = toRemove->End()->Y;
                        }
                    }
                }
                x = max(0, min(BoardSize - 1, x + random(2 * randomRange + 1) - randomRange));
                y = max(0, min(BoardSize - 1, y + random(2 * randomRange + 1) - randomRange));
            }

            // redo connections with some randomness
            vector<Cell*> ends;
            for (Path* path : undone) {
                ends.push_back(path->Start());
                ends.push_back(path->End());
            }
            for (Cell* c : empty) ends.push_back(c);

            for (int i = 0; i < ends.size(); i++) scores[i] = ends[i]->Value + random(9);
            for (int i = 0; i < ends.size(); i++) {
                for (int j = 1; j < ends.size(); j++) {
                    if (scores[j - 1] < scores[j]) {
                        double tmpScore = scores[j - 1];
                        scores[j - 1] = scores[j];
                        scores[j] = tmpScore;
                        Cell* tmpCell = ends[j - 1];
                        ends[j - 1] = ends[j];
                        ends[j] = tmpCell;
                    }
                }
            }
            vector<Path*> replacements;
            vector<Path*> cross;
            int newScore = 0;
            for (Cell* cell : ends) {
                if (cell->Paths.size() > 0) continue;
                bool hasPartner = false;
                for (Cell* e : ends) {
                    if (e != cell && e->Color == cell->Color && e->Paths.size() == 0) hasPartner = true;
                }
                if (!hasPartner) continue;
                cell->BFS(crossings);
                vector<Cell*> partners;
                for (Cell* c : ends) {
                    if (c != cell && c->Color == cell->Color && c->Paths.size() == 0 && dist[c->ID + BoardArea * crossings] > 0) partners.push_back(c);
                }
                if (partners.size() == 0) continue;
                double power = randomDouble();
                double score = -100;
                Cell* toConnect = nullptr;
                for (int p = 0; p < partners.size(); p++) {
                    int c = 0;
                    while (dist[partners[p]->ID + BoardArea * c] == 0) c++;
                    double s = cell->Value * partners[p]->Value + random(5) - BoardPenalty * c;
                    s = s / pow(dist[partners[p]->ID + BoardArea * c], power);
                    if (s > score && (toConnect == nullptr || random(10) > 0)) {
                        score = s;
                        toConnect = partners[p];
                    }
                }
                Path* path = cell->BuildPath(toConnect, dist);
                if (path->Score() > 0) {
                    replacements.push_back(path);
                    newScore += path->Score();
                    for (Cell* c : path->Cells) {
                        for (int i = 0; i < c->Paths.size() - 1; i++) {
                            if (find(replacements.begin(), replacements.end(), c->Paths[i]) == replacements.end() && find(cross.begin(), cross.end(), c->Paths[i]) == cross.end()) cross.push_back(c->Paths[i]);
                        }
                    }
                } else {
                    path->Remove();
                    freePaths.push(path);
                }
            }

            // redo crossings between old and new paths
            for (Path* p : cross) {
                oldScore += p->Remove();
                p->Start()->BFS(crossings);
                if (dist[p->End()->ID + crossings * BoardArea] == 0) {
                    p->Apply();
                    oldScore -= p->Score();
                    continue;
                }
                undone.push_back(p);
                Path* r = p->Start()->BuildPath(p->End(), dist);
                newScore += r->Score();
                replacements.push_back(r);
            }

            // keep the better
            if (newScore < oldScore) {
                for (Path* p : replacements) {
                    p->Remove();
                    freePaths.push(p);
                }
                for (Path* p : undone) p->Apply();
            } else {
                for (Path* p : undone) {
                    result.erase(std::remove(result.begin(), result.end(), p), result.end());
                    freePaths.push(p);
                }
                for (Path* p : replacements) result.push_back(p);
                empty.clear();
                for (Cell* e : ends) {
                    if (e->Paths.size() == 0) empty.push_back(e);
                }
                totalScore += newScore - oldScore;
                if (newScore > oldScore) lastImprovement = runs;
            }
        }

        if (runs % 64 == 0) {
            int dir = (runs / 64) % 4;
            auto compPath = [&](Path* a, Path* b) -> bool { return a->Start()->X * dx[dir] + a->Start()->Y * dy[dir] > b->Start()->X * dx[dir] + b->Start()->Y * dy[dir]; };
            sort(result.begin(), result.end(), compPath);
            // for (Path* path : result.OrderBy(r => -r.Start.X * Cell.dx[dir] - r.Start.Y * Cell.dy[dir]).ToList())
            for (Path* path : result) {
                path->Remove();
                path->Start()->BFS(5);
                if (dist[path->End()->ID + 5 * BoardArea] == 0) {
                    totalScore += path->Score();
                    path->Apply();
                    continue;
                }
                Path* newPath = path->Start()->BuildPath(path->End(), dist, dir);
                result.erase(std::remove(result.begin(), result.end(), path), result.end());
                result.push_back(newPath);
            }
            totalScore = 0;
            for (Path* path : result) totalScore += path->Start()->Value * path->End()->Value;
            for (int y = 0; y < BoardSize; y++) {
                for (int x = 0; x < BoardSize; x++) {
                    totalScore -= max(BoardPenalty * (int)(BoardGrid[x][y].Paths.size() - 1), 0);
                }
            }
        }

        cerr << "runs: " << runs << "   score: " << totalScore << "   BFS total: " << bfsCount << endl;
    }

    void Solve(vector<Path*>& result) {
        int freq[10] = {};
        for (int y = 0; y < BoardSize; y++) {
            for (int x = 0; x < BoardSize; x++) {
                freq[BoardGrid[x][y].Color]++;
            }
        }

        vector<Cell*> withValue;
        for (int y = 0; y < BoardSize; y++) {
            for (int x = 0; x < BoardSize; x++) {
                if (BoardGrid[x][y].Value > 0 && freq[BoardGrid[x][y].Color] > 1) withValue.push_back(&BoardGrid[x][y]);
                freq[BoardGrid[x][y].Color]++;
            }
        }

        start = chrono::high_resolution_clock::now();
        int bestScore = 0;
        int attempts = 10;
        int totalTime = 9800;
        int intervalTime = totalTime / attempts;
        for (int i = 0; i < attempts; i++) {
            vector<Path*> current;
            GreedyInit(withValue, current);
            Mutate(current, withValue, i * intervalTime, intervalTime, totalTime - i * intervalTime);
            int score = 0;
            for (Path* path : current) score += path->Remove();
            if (score > bestScore) {
                bestScore = score;
				for (Path* path : result) freePaths.push(path);
                result.clear();
                for (Path* path : current) result.push_back(path);
            }
        }
    }
};

int main() {
    for (int i = 0; i < 30 * 30 * 15; i++) freePaths.push(&paths[i]);

    Board board;
    board.ReadInput();

    vector<Path*> solution;
    board.Solve(solution);
    cout << solution.size() << endl;
    for (Path* path : solution) cout << path->Print() << endl;
}