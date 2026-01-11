Problem statement: https://www.topcoder.com/challenges/8fe73e58-d883-41bd-9028-b1c925c380e5?tab=details

By default every car is allowed to move. This rule has a few heuristic exceptions:
## Cycles
Before the first turn, I detect all possible cycles up to a size of 24.
A cycle is a set of cells that can cause a permanent traffic jam.
Within a cycle it can be dangerous to have cars facing all 4 directions. Therefore I disallow entering a cycle if this would risk getting stuck.

## Blocked crossings
There are situations when a car is right before a crossing and the next cell is free. However it's clear that the car can't pass all the crossings and get to the other side of the intersection without waiting and blocking others. Therefore a car will wait in this situation as well.

## Priorities
Two cars might want to enter the same cell at the same time. Here the car which currently causes the bigger jam gets priority.

## Simulations
The previous heuristics should score in the region of 97 points already. To further Improve from there, I look at every car that would not move in this turn and try to force it to move before applying the rest of the heuristics. A simulation for the next 2*N turns (using above heuritics for all further actions, without simulating spawns) will decide if cars reach their targets faster (or unblock spawn points) and thus overwrite the heuristics. This can also make a car enter a cycle which is considered risky - but it's fine in most cases.
However there can be an unfortunate spawn situation that causes the unspeakable to happen.
When I detect such a jam coming up, I also try to force a car to stop and see if it helps to avoid the jam.

## Scores
My first 10 scores:
```
Seed =  1, Score =  4469
Seed =  2, Score = 23816
Seed =  3, Score = 12300
Seed =  4, Score =  9299
Seed =  5, Score =  8417
Seed =  6, Score =  8491
Seed =  7, Score = 12101
Seed =  8, Score = 18032
Seed =  9, Score = 11756
Seed = 10, Score =  2794
```

There are still performance issues that I couldn't solve. on seed 440 I would like to take about 40s offline, so probably even a bit longer on TC servers. I have to turn off my simulation and jam avoidance in this case, causing me to only get about 40% of my potential score with more computation time.