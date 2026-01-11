Solution to https://www.topcoder.com/challenges/883d6bcd-3749-427a-964a-ac8435737228?tab=details

The overall approach is a beam search with a width between 1300 and 4000, depending on the testcase.
A state is defined by the remaining guards, coins and the thief's position. For equality check the last 7 bits of the thief's coordinate are set to 0, thus only keeping one beam node per 128x128 region.

As a preprocessing I analyze each combination of guard and coin to see how protective a guard is of a coin. In some cases it's impossible to reach a coin, grab it and run away without the guard noticing - making that guard a prime target for bribery. Some other guards also only leave a narrow time window to get the coin, making their bribing more worthy than for other guards.

While tracking the real score at every turn, there is also a heuristic component, that rewards closeness to the nearest coin (or to the edge in the absence of coins). Furthermore I reward having a guard bribed as long as that guard still protects coins. When the coins get collected, the score reward gets removed as well.

Scores:
```
Test Case #01: Score = 1218
Test Case #02: Score = 29048
Test Case #03: Score = 12975
Test Case #04: Score = 5045
Test Case #05: Score = 4745
Test Case #06: Score = 11943
Test Case #07: Score = 11460
Test Case #08: Score = 1779
Test Case #09: Score = 19191
Test Case #10: Score = 16270
```