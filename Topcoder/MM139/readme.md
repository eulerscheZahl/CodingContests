Problem statement: https://www.topcoder.com/challenges/05bd815f-0e25-4268-a02d-7b8233d710dc?tab=details

My solution is some greedy init followed by a hill climbing for higher scores. I ported it from C# to C++ for a gain of roughly 0.15 points at the end.

The hill climbing removes 8 pipes at a time (with preference on same color and nearby pipes). It then finds new partners to connect via BFS. When there are multiple candidates, it uses `score * pow(dist, f)`, where `f` is randomly chosen between 0 and 1 after each iteration. If the new score is greater or equal to the previous one, the new pipes will  replace the old ones.
The allowed number of crossings per newly generated path starts at 0 and then goes up over time.

Scores:
```
Test Case  #1: Score =  309
Test Case  #2: Score = 1587
Test Case  #3: Score = 2691
Test Case  #4: Score = 3469
Test Case  #5: Score = 3284
Test Case  #6: Score =  589
Test Case  #7: Score = 1402
Test Case  #8: Score = 1154
Test Case  #9: Score = 2263
Test Case #10: Score = 2045
```
