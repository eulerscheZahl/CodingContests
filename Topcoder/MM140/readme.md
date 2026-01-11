Problem statement: https://www.topcoder.com/challenges/d6b59637-343d-47b3-ac1e-8b45b2416b09?tab=details

A beam search with some pruning of legal actions: no jumps at first and always moving to the end of a line to keep branching low.
The existance of loops is checked during the expansion of each node.

A nice gotcha that's not part of my beam scoring and only checked for afterwards: you don't necessarily need identical actions for a loop, you can split them in some cases, e.g. replace a `F 10` by `F 4` and `F 6` if it helps the loops.

Scores:
```
Test Case  #1: Score =   9
Test Case  #2: Score = 226
Test Case  #3: Score =  77
Test Case  #4: Score =  56
Test Case  #5: Score =  16
Test Case  #6: Score =  35
Test Case  #7: Score =  96
Test Case  #8: Score =  67
Test Case  #9: Score =  72
Test Case #10: Score =  51
```
