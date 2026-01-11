#pragma GCC optimize("-O3", "-ffast-math")
#pragma GCC optimize("inline")
#pragma GCC optimize("omit-frame-pointer")
#pragma GCC optimize("unroll-loops")

#include <cstring>
#include <iostream>
#include <string>
#include <vector>
#include <cmath>
#include <chrono>
using namespace std;

int MAX_TIME = 6000;
int Size;
double Center;
int BallCount;
int BonusValue;
char inputGrid[30][30];
char outputGrid[30][30];
char simBackup[30][30];

char Empty = '.';
char Bonus = '*';
char panel1 = '/';
char panel2 = '\\';
int dx[] = {-1, 0, 1, 0};
int dy[] = {0, -1, 0, 1};
int changeDir1[] = {3, 2, 1, 0}; // D / L, R / U, U / R, L / D
int changeDir2[] = {1, 0, 3, 2}; // D \ R, R \ D, U \ L, L \ U
int panelValue = 1;              // value of hitting a panel
chrono::_V2::system_clock::time_point start;

class Loc
{
public:
    int X;
    int Y;
    int Dir;

    Loc() {}

    Loc(int x, int y)
    {
        this->X = x;
        this->Y = y;
    }

    int Key()
    {
        return (X + 1) * (Size + 2) + (Y + 1);
    }
};

Loc Guns[5];

Loc RandomGunSpot()
{
    int type = rand() % 4;
    int loc = rand() % Size;
    if (type == 0)
        return Loc(loc, -1);
    if (type == 1)
        return Loc(loc, Size);
    if (type == 2)
        return Loc(-1, loc);
    return Loc(Size, loc);
}

void board()
{
    for (int i = 0; i < BallCount; i++)
    {
        Guns[i] = RandomGunSpot();
        for (int j = 0; j < i; j++)
        {
            if (Guns[i].X == Guns[j].X && Guns[i].Y == Guns[j].Y)
            {
                i--;
                break;
            }
        }
    }

    for (int y = 0; y < Size; y++)
    {
        for (int x = 0; x < Size; x++)
        {
            outputGrid[x][y] = inputGrid[x][y];
        }
    }
}

bool inGrid(int x, int y)
{
    return x >= 0 && x < Size && y >= 0 && y < Size;
}

int visited[30][30];
int sims = 0;
int multiple[2000];
double dist[2];
int Simulate(bool final = false)
{
    sims++;
    memset(visited, 0, 30 * 30 * sizeof(int));
    memcpy(simBackup, outputGrid, 30 * 30 * sizeof(char));
    int bonusHits = 0, panelHits = 0;
    Loc Balls[BallCount];
    for (int i = 0; i < BallCount; i++)
    {
        int dir = -1;
        if (Guns[i].Y == -1)
            dir = 3;
        else if (Guns[i].Y == Size)
            dir = 1;
        else if (Guns[i].X == -1)
            dir = 2;
        else
            dir = 0;

        Balls[i] = Loc(Guns[i].X, Guns[i].Y);
        Balls[i].Dir = dir;
    }

    // simulate and compute score
    while (true)
    {
        for (int i = 0; i < BallCount; i++)
            multiple[Balls[i].Key()] = 0;
        for (int i = 0; i < BallCount; i++)
        {
            Balls[i].Y += dy[Balls[i].Dir];
            Balls[i].X += dx[Balls[i].Dir];
            int x = Balls[i].X;
            int y = Balls[i].Y;

            // update hits
            if (inGrid(x, y))
            {
                if (outputGrid[x][y] == Empty)
                {
                    string panels = "/\\";
                    for (int p = 0; p < 2; p++)
                    {
                        outputGrid[x][y] = panels[p];
                        int x_ = x, y_ = y, dir = Balls[i].Dir;
                        for (int depth = 0; depth < 3; depth++)
                        {
                            if (outputGrid[x_][y_] == panel1)
                                dir = changeDir1[dir];
                            else if (outputGrid[x_][y_] == panel2)
                                dir = changeDir2[dir];
                            x_ += dx[dir];
                            y_ += dy[dir];
                            if (!inGrid(x_, y_) || outputGrid[x_][y_] == Empty)
                                break;
                        }
                        if (!inGrid(x_, y_))
                            dist[p] = 1e9;
                        dist[p] = sqrt((x_ - Center) * (x_ - Center) + (y_ - Center) * (y_ - Center));
                    }
                    if (dist[0] < dist[1])
                        outputGrid[x][y] = panels[0];
                    else
                        outputGrid[x][y] = panels[1];
                    if (final)
                        simBackup[x][y] = outputGrid[x][y];
                }
                if (outputGrid[x][y] == panel1 || outputGrid[x][y] == panel2)
                    panelHits++;
                if (outputGrid[x][y] == Bonus)
                    bonusHits++;
            }
            multiple[Balls[i].Key()]++;
        }

        for (int i = 0; i < BallCount; i++)
        {
            int x = Balls[i].X;
            int y = Balls[i].Y;
            if (!inGrid(x, y))
            {
                memcpy(outputGrid, simBackup, 30 * 30 * sizeof(char));
                for (int j = 0; j < BallCount; j++)
                    multiple[Balls[j].Key()] = 0;
                return bonusHits * BonusValue + panelHits * panelValue;
            }
            visited[x][y]++;

            // perform the bounce
            // NOTE: do not rotate a panel if it is hit by multiple balls at the same time
            if (outputGrid[x][y] == panel1)
            {
                Balls[i].Dir = changeDir1[Balls[i].Dir];
                if (inputGrid[x][y] == Empty && multiple[Balls[i].Key()] == 1)
                    outputGrid[x][y] = panel2;
            }
            else if (outputGrid[x][y] == panel2)
            {
                Balls[i].Dir = changeDir2[Balls[i].Dir];
                if (inputGrid[x][y] == Empty && multiple[Balls[i].Key()] == 1)
                    outputGrid[x][y] = panel1;
            }
        }
    }
}

void PrintSolution()
{
    string sol = "";
    for (int y = 0; y < Size; y++)
    {
        for (int x = 0; x < Size; x++)
            sol += outputGrid[x][y];
        sol += "\n";
    }
    cerr << sol << endl;

    Simulate(true); // for visit counts
    for (int i = 0; i < BallCount; i++)
        cout << Guns[i].Y << " " << Guns[i].X << endl;
    sol = "";
    string heat = "";
    for (int y = 0; y < Size; y++)
    {
        for (int x = 0; x < Size; x++)
        {
            cout << outputGrid[x][y] << endl;
            sol += outputGrid[x][y];
            heat += to_string(visited[x][y]) + " ";
        }
        heat += "\n";
        sol += "\n";
    }
    cerr << sol << endl;
    cerr << heat << endl;
}

void Hillclimb(int region)
{
    int bestScore = Simulate();
    char backup[30][30];
    int bestVisited[30][30];
    memcpy(backup, outputGrid, 30 * 30 * sizeof(char));
    memcpy(bestVisited, visited, 30 * 30 * sizeof(int));
    int lastImprovement = 0;
    for (int i = 0; i - lastImprovement < 50 * Size; i++)
    {
        if (i % 1024 == 0 && chrono::duration_cast<chrono::milliseconds>(chrono::high_resolution_clock::now() - start).count() > MAX_TIME)
            break;
        int x = rand() % (Size - region + 1), y = rand() % (Size - region + 1);
        if (bestVisited[x][y] > rand() % Size)
        {
            i--;
            continue;
        }
        bool changed = false;
        for (int dx = 0; dx < region; dx++)
        {
            for (int dy = 0; dy < region; dy++)
            {
                if (rand() % 2 == 0)
                    continue;
                int x_ = x + dx, y_ = y + dy;
                if (inputGrid[x_][y_] != Empty)
                    continue;
                if ((i < 15 * Size || i - lastImprovement < 20) && bestVisited[x_, y_] == 0)
                    continue;
                outputGrid[x_][y_] = (outputGrid[x_][y_] == '/' || outputGrid[x_][y_] == Empty && rand() % 2 == 0) ? '\\' : '/';
                changed |= bestVisited[x_, y_] > 0;
            }
        }
        if (!changed)
        {
            i--;
            continue;
        }
        int score = Simulate();
        if (score <= bestScore)
        {
            for (int dx = 0; dx < region; dx++)
            {
                for (int dy = 0; dy < region; dy++)
                {
                    int x_ = x + dx, y_ = y + dy;
                    outputGrid[x_][y_] = backup[x_][y_];
                }
            }
        }
        else
        {
            if (score > bestScore)
                lastImprovement = i;
            for (int dx = 0; dx < region; dx++)
            {
                for (int dy = 0; dy < region; dy++)
                {
                    int x_ = x + dx, y_ = y + dy;
                    backup[x_][y_] = outputGrid[x_][y_];
                }
            }
            memcpy(bestVisited, visited, 30 * 30 * sizeof(int));
            bestScore = score;
        }
    }
}

// https://machinelearningmastery.com/simulated-annealing-from-scratch-in-python/
void Anneal()
{
    int bestScore = Simulate();
    int initial = bestScore;
    char bestBackup[30][30];
    int bestVisited[30][30];
    memcpy(bestBackup, outputGrid, 30 * 30 * sizeof(char));
    memcpy(bestVisited, visited, 30 * 30 * sizeof(int));

    int currScore = bestScore;
    char currBackup[30][30];
    int currVisited[30][30];
    memcpy(currBackup, outputGrid, 30 * 30 * sizeof(char));
    memcpy(currVisited, visited, 30 * 30 * sizeof(int));
    int lastImprovement = 0;

    int x = rand() % Size, y = rand() % Size;
    for (int i = 0; i < 50 * Size; i++)
    {
        int dir = rand() % 4;
        x += dx[dir];
        y += dy[dir];
        while (!inGrid(x, y))
        {
            x = rand() % Size;
            y = rand() % Size;
        }
        if (i % 1024 == 0 && chrono::duration_cast<chrono::milliseconds>(chrono::high_resolution_clock::now() - start).count() > MAX_TIME)
            break;
        if (inputGrid[x][y] == Empty && currVisited[x][y] != 0 && rand() % 2 == 0)
        {
            if (rand() % 2 == 0)
                outputGrid[x][y] = Empty;
            else
                outputGrid[x][y] = (outputGrid[x][y] == '/' || outputGrid[x][y] == Empty && rand() % 2 == 0) ? '\\' : '/';
        }
        else
        {
            i--;
            continue;
        }
        int score = Simulate();
        int diff = score - currScore;
        double t = Size / (1.0 + i);
        double metropolis = exp(diff / t);
        if (diff > 0 || rand() < metropolis * RAND_MAX)
        {
            currBackup[x][y] = outputGrid[x][y];
            memcpy(currVisited, visited, 30 * 30 * sizeof(int));
            currScore = score;
            if (currScore >= bestScore)
            {
                lastImprovement = i;
                // cerr << endl
                //      << currScore << endl;
                bestScore = currScore;
                memcpy(bestVisited, currVisited, 30 * 30 * sizeof(int));
                memcpy(bestBackup, outputGrid, 30 * 30 * sizeof(char));
            }
        }
        else
        {
            memcpy(outputGrid, currBackup, 30 * 30 * sizeof(char));
        }
        if (lastImprovement > 30)
        {
            currScore = bestScore;
            memcpy(currVisited, bestVisited, 30 * 30 * sizeof(int));
            memcpy(outputGrid, bestBackup, 30 * 30 * sizeof(char));
            while (currVisited[x][y] > Size || currVisited[x][y] == 0)
            {
                x = rand() % Size;
                y = rand() % Size;
            }
        }
    }
    memcpy(outputGrid, bestBackup, 30 * 30 * sizeof(char));
    // cerr << initial << " => " << bestScore << endl;
}

int main()
{
    cin >> Size;
    Center = (Size - 1) / 2.0;
    cin >> BallCount;
    cin >> BonusValue;

    int x = 0, y = 0;
    while (y < Size)
    {
        string line;
        cin >> line;
        for (int i = 0; i < line.length(); i++)
        {
            inputGrid[x][y] = line[i];
            if (++x == Size)
            {
                x = 0;
                y++;
            }
        }
    }

    int bestScore = -1;
    char bestGrid[30][30];
    Loc bestGuns[5];
    int runs = 0;
    int inits = 1000;
    start = chrono::high_resolution_clock::now();
    while (chrono::duration_cast<chrono::milliseconds>(chrono::high_resolution_clock::now() - start).count() < MAX_TIME)
    {
        runs++;
        inits--;
        board();
        char runGrid[30][30];
        Loc runGuns[5];
        memcpy(runGrid, outputGrid, 30 * 30 * sizeof(char));
        memcpy(runGuns, Guns, 5 * sizeof(Loc));
        int roundScore = Simulate();
        for (int i = 0; i < inits; i++)
        {
            board();
            int tmpScore = Simulate();
            if (tmpScore > roundScore)
            {
                memcpy(runGrid, outputGrid, 30 * 30 * sizeof(char));
                memcpy(runGuns, Guns, 5 * sizeof(Loc));
                roundScore = tmpScore;
            }
        }
        memcpy(outputGrid, runGrid, 30 * 30 * sizeof(char));
        memcpy(Guns, runGuns, 5 * sizeof(Loc));
        Hillclimb(2);
        // Anneal();
        int score = Simulate();
        if (score > bestScore)
        {
            bestScore = score;
            memcpy(bestGrid, outputGrid, 30 * 30 * sizeof(char));
            memcpy(bestGuns, Guns, 5 * sizeof(Loc));
        }
    }
    memcpy(outputGrid, bestGrid, 30 * 30 * sizeof(char));
    memcpy(Guns, bestGuns, 5 * sizeof(Loc));
    MAX_TIME = 9500;
    while (chrono::duration_cast<chrono::milliseconds>(chrono::high_resolution_clock::now() - start).count() < MAX_TIME)
        Hillclimb(Size <= 10 ? 3 : 2);
    bestScore = Simulate();
    cerr << bestScore << " @" << runs << "," << sims << endl;
    PrintSolution();
}