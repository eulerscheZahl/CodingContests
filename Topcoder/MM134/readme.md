Task: https://www.topcoder.com/challenges/5ac77343-7f2f-4635-8aff-bbf49c700fda?tab=details

# Attractors
My strategy is to define an attractor for each sheep and barn on the map. That means I reward getting close to that entity. The value decreases with higher distance. For sheep the value also depends on the wool it currently has - not in a linear way, as it also takes time to catch the sheep. The distance mostly uses a BFS distance which sets all sheep to trees and other farmers to empty cells. It also contains Euclidean distance divided by Manhattan distance to promote diagonal paths to the target over straight lines, as they are less likely to be blocked by sheep (or at least there's an alternative path with the same length remaining).

The exact details and coefficients are a lot of trial and error.

# Move Generation
The attractors above allow to compute a score for any given state. I generate an initial, greedy solution which just moves each farmer to the best sheep or to the closest barn if the farmer can't carry any more wool.
Then I bruteforce all the actions for each farmer, one by one, while keeping the best known action for the remaining farmers. This bruteforce tries the farmer both as first and last one in the list to account for the order of actions. The outcome of those actions is simulated (without moving any sheep) to get a resulting state and thus a score.
If a farmer moves into another farmer, the actions of this second farmer will also be subject to an exhaustive search.

# Collaboration between farmers
I tried a few things that didn't work out, such as assigning a region to each farmer and then keeping them in their respective region.
In the end I just down-scaled the attractor values with a constant factor, if there is any other farmer closer to a given sheep.


# My Scores
```
Test Case  #1: Score =  260
Test Case  #2: Score = 6051
Test Case  #3: Score = 4276
Test Case  #4: Score =  398
Test Case  #5: Score = 1242
Test Case  #6: Score =  207
Test Case  #7: Score = 2119
Test Case  #8: Score =  622
Test Case  #9: Score = 1390
Test Case #10: Score =  383
```

# Implementation
The most is boilerplate. The only part that might be remotely interesting is here: https://github.com/eulerscheZahl/Topcoder/blob/master/MM134/Plan.cs
