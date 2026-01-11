# MM144 - Flood
Statement: https://www.topcoder.com/challenges/d8803445-3ae4-4ce6-9949-6fea9313368e?tab=details

My solution is split into two parts: generate a list of cells that shall be defended and find a plan that moves builders accordingly. This process is repeated until there is no more time left. On seed 2 that's about 150 completely independent searches.

## Finding protect-worthy cells
Here we start with a small list of cells, e.g. all starting locations of builders. I add a bit of randomness like protecting a cell next to the builder instead or not trying to defend all of those cells.
Then I try to find a valid plan (more on that below), how to protect those cells. If such a plan exists, I add one more cell to defend - this new cell will border one of my currently defended cells. If the cell can't be added, I remove it again and try another cell. If I fail to increase the region for 20 turns, I give up and start with a completely new iteration of just a few cells to protect.

## Moving builders
Given a list of cells to defend, it's easy to find where to place the walls: place a wall at each cell bordering the protected region. Simulate the water, then remove all cells again, that won't get touched by the water.
Now let's move the builders and place the walls we need. First I give each builder a list of cells to visit (initially only the starting location) and an approximate time it takes. Then I greedily add whatever pair of builder and wall results in the shortest total time (taking the time into account, that the builder already spends with other wall placements). I ignore path optimizations and instead pretend that the builder has to visit a cell to place a box (while in reality the builder has to walk next to it and can have a shorter path to the next cell that way).
Now that each builder knows which walls to place, we have to find an actual path for each builder individually. That's done by making each builder walk to the walls in the list and stopping when only 1 cell away. When we have a path that allows to place walls everywhere, we loop through that list of cells in reverse order and perform each wall placement after we leave a cell for the last time (to not block our own path, if we have to ge through the same cell twice).
Then we have to piece the paths for each builder together. This possibly means letting a builder wait for another one to finish first or even realize that it's not possible and there is no valid solution. Of course it can also happen, that the water is simply faster.

## Combining solutions
Sometimes you have different builders that each defend their own territory (or some of them team up, while others are isolated). It's unlikely that you get the best plan for all of those distinct groups in the same run. Therefore I extract solutions for groups of builders and try to put them together to a better overall solution

## Scores
On seed 78 I only get a score of 95, which is significantly worse than the 107 from the animation in the statement. My move generator is unable to find the amount of teamwork done around the bottom tap.
```
Test Case  #1: Score =  43
Test Case  #2: Score = 503
Test Case  #3: Score = 462
Test Case  #4: Score = 100
Test Case  #5: Score = 607
Test Case  #6: Score =  55
Test Case  #7: Score = 292
Test Case  #8: Score = 122
Test Case  #9: Score = 313
Test Case #10: Score = 339
```
