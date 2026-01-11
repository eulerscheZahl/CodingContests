Task: https://codeforces.com/contest/2177/problem/A

My scores:
```
#1: OK [109ms, 1MB]: 1 50 1 0 1 0 1 0 1 0 points 6350.0 Solution is correct
#2: OK [93ms, 1MB]: 1 50 1 25 1 0 1 0 1 0 points 6275.0 Solution is correct
#3: OK [125ms, 1MB]: 1 46.875 1 59.375 1 21.875 1 25 1 0 points 6040.625 Solution is correct
#4: OK [3187ms, 41MB]: 1 50 1 79.375 1 52.7865 1 55.1953 1 43.4115 points 5657.6953125 Solution is correct
#5: OK [3828ms, 46MB]: 2 25.9277 1 54.9927 1 38.7451 1 38.6963 1 32.0068 points 5428.8940429688 Solution is correct
#6: OK [3796ms, 42MB]: 2 25.6226 1 53.9062 1 28.9917 1 27.9175 1 26.8311 points 5510.1928710938 Solution is correct
#7: OK [3656ms, 42MB]: 1 50 1 71.875 1 42.3307 1 44.4922 1 40.0651 points 5753.7109375 Solution is correct
#8: OK [3687ms, 38MB]: 1 50 1 73.737 1 45.3646 1 37.5521 1 25.5339 points 5803.4375 Solution is correct
#9: OK [3828ms, 42MB]: 1 50 1 82.2632 1 41.4673 1 52.3315 1 58.0322 points 5647.7172851562 Solution is correct
#10: OK [3781ms, 42MB]: 1 49.9878 1 53.1738 1 59.3384 1 44.8608 1 47.8394 points 5734.3994140625 Solution is correct
#11: OK [3515ms, 40MB]: 1 50 1 69.401 1 43.8411 1 45.0521 1 51.6536 points 5720.15625 Solution is correct
#12: OK [3171ms, 29MB]: 4 37.5977 4 38.6719 4 23.0957 4 11.7432 4 4.90723 points 4901.953125 Solution is correct
#13: OK [2093ms, 29MB]: 4 38.2324 4 39.3799 4 22.1924 4 11.9385 4 9.25293 points 4887.01171875 Solution is correct
#14: OK [2343ms, 29MB]: 4 38.1348 4 28.0029 4 38.4033 4 8.8623 4 22.0459 points 4843.65234375 Solution is correct
#15: OK [2593ms, 28MB]: 4 37.6465 4 34.9365 4 25.415 4 4.39453 4 26.8799 points 4862.1826171875 Solution is correct
#16: OK [1500ms, 28MB]: 4 38.2324 4 40.2588 4 25.8789 4 18.9209 4 17.8711 points 4826.513671875 Solution is correct
#17: OK [2484ms, 29MB]: 4 37.5977 4 20.7764 4 17.1387 4 13.7207 4 15.21 points 4936.669921875 Solution is correct
#18: OK [2187ms, 28MB]: 4 37.6221 4 32.9834 4 13.7939 4 21.3623 4 10.8398 points 4900.1953125 Solution is correct
#19: OK [1968ms, 28MB]: 4 38.2324 4 43.8232 4 6.46973 4 8.61816 4 12.8174 points 4920.1171875 Solution is correct
#20: OK [3140ms, 29MB]: 8 43.75 8 57.0801 8 57.3242 8 14.5508 8 3.61328 points 5346.044921875 Solution is correct
#21: OK [2062ms, 29MB]: 8 46.875 8 70.3125 8 3.02734 8 11.5723 8 5.46875 points 5463.232421875 Solution is correct
#22: OK [4218ms, 30MB]: 8 43.75 8 49.707 8 8.49609 8 7.37305 8 11.2305 points 5513.330078125 Solution is correct
#23: OK [1515ms, 29MB]: 8 46.875 8 50.4395 8 39.209 8 35.4004 8 15.0879 points 5313.96484375 Solution is correct
```

## Approach:

There are some cases where a conflict of 1 is possible. Failing this costs too many points, so let's focus on that at first.
For a task it's mostly important, from which group to which other group it shall be transferred. The exact leaf is a minor detail.
I split the problem into 2 phases. First I assign tasks to planes, making sure that each leaf is only used as often, that the individual plane can still be solved (function `BuildPlaneGroups`).
With the rough assignment of tasks to planes done, I randomly assign spines to each task and then assign tasks to OXCs (function `SolveOxcs`), solving one plane after another.
The exact linking inside the OXC is then trivial and can be done in a greedy manner.

Solving a group (first tasks to planes, then takes within the same plane to OXCs - same code for both: `SolveContainers`) is done by assigning tasks to each container (=plane or OXC) randomly.
Then I take a random OXC that violates the constraints (uses a leaf or spine too many times) and just put that task to another container - preferring one that has at least one of the task's leaves free.
When you just keep this running, you find a solution in most cases.

To avoid high rearrangement costs, I track previous tasks and their routes, then find tasks of the current query with the same source and target group.
A container initially has some tasks that can't be moved to other containers in order to reuse those existing connections.
When the search keeps struggling, I allow to move those tasks as well, one at a time.

After a solution is found, I try to further improve (merged.cs, line 1035) by scrambling some of the containers and then only solving those again.

For the 1:3 and 1:7 cases I just remove some tasks (function `ReduceTasks`), treat the problem as a 1:1 and add the functions back (`RestoreTasks`).
Here it's possible to allow the same leaf-spine path multiple times in the 1:1 solver, as a conflict of 1 isn't possible anyway.



## Improvements:
I have 2 separate phases that are completely decoupled. While solving one plane at a time, it would be possible to still swap a few tasks and reuse more OXC connections that way.
I realized that an hour after the contest ended.
