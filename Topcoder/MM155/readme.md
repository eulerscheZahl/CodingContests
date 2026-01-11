## Strategy
I start with some static analysis of the board in order to reduce the number of arcs in the graph. E.g. if a cell `c` only has 1 possible next step `n`, it's often beneficial to remove all other previous cells of `n`. There are some caveats of course, like multiple cells having `n` as the only next step.

An initial solution is then generated via beam search. Here I go in reverse order, as that makes it easier to reward high-value cells at the end of the path.
From then on I randomly remove about 10 consecutive nodes of the path and try to close the gap again via beam search. This approach is likely to find the same new connection again, that has just been removed, therefore I sometimes randomly disallow a cell from the old path and thereby force a different new path.
This process of removing and replacing parts of the path is repeated in a hill-climbing manner. But it's allowed to get a slightly worse score. If the score drops too far, I'll just reset to the best known solution and then keep mutating from there.


## Scores
As achieved on TC servers by my last submit
```
Test Case  #1: Score =   3270
Test Case  #2: Score = 549640
Test Case  #3: Score = 507305
Test Case  #4: Score = 667055
Test Case  #5: Score = 449608
Test Case  #6: Score =  74011
Test Case  #7: Score = 210882
Test Case  #8: Score = 146253
Test Case  #9: Score = 282449
Test Case #10: Score = 132188
```
