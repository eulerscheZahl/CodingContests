Problem statement: https://www.topcoder.com/challenges/81d26476-fb3c-4e4e-9ffe-7ee17c0438ab?tab=details

I approached this problem with a beam search. The side values are not fixed, but recomputed during each step of the beam search in order to maximize the score.

On smaller maps that's all I do: pick a starting point at random and run the beam until there are no more valid moves. Then repeat it again from another starting point as long as there is time.

For larger maps I split the whole board into smaller regions and use a hardcoded path across them to make sure that I can reach the starting point again and get the loop bonus.
In this case I have 2 nested beam searches. The inner one is just responsible for reaching the next region. The outer one filters those paths that reach the next region.
It's only allowed to visit the current region, any leftovers of a previous region and the next region (in which case the inner beam stops).

This is done for 3/4 of the time and then followed by some local optimizations for the paths with the highest scores: a beam search run on two consecutive regions in the hope to find a higher score when merging them. This step is performed in both directions, as the beam might cut off a good path because of a poor start.

Scores:
```
Seed =  1, Score =   54
Seed =  2, Score = 1965
Seed =  3, Score = 1377
Seed =  4, Score =  271
Seed =  5, Score =   81
Seed =  6, Score =  322
Seed =  7, Score =  411
Seed =  8, Score = 1505
Seed =  9, Score = 1220
Seed = 10, Score = 1485
```
