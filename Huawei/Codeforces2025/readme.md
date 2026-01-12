Task: https://codeforces.com/contest/2177/problem/A

Rank 20, 123.9k points. My scores:
```
#1: OK [109ms, 1MB]: 1 50 1 0 1 0 1 0 1 0 points 6350.0 Solution is correct
#2: OK [171ms, 7MB]: 1 50 1 75 1 0 1 0 1 50 points 5975.0 Solution is correct
#3: OK [109ms, 4MB]: 1 46.875 1 40.625 1 37.5 1 0 1 18.75 points 6068.75 Solution is correct
#4: OK [3718ms, 37MB]: 1 50 1 85.5469 1 41.0026 1 48.9193 1 42.2135 points 5696.953125 Solution is correct
#5: OK [4015ms, 42MB]: 2 25.9399 1 57.3364 1 44.7632 1 35.9497 1 42.9199 points 5379.2724609375 Solution is correct
#6: OK [3734ms, 50MB]: 2 25.592 1 62.5916 1 26.2329 1 30.6519 1 24.4995 points 5491.2963867188 Solution is correct
#7: OK [3390ms, 42MB]: 1 50 1 77.5651 1 24.8047 1 28.763 1 40.8203 points 5834.140625 Solution is correct
#8: OK [3750ms, 38MB]: 1 50 1 66.849 1 50.4557 1 33.2422 1 37.3568 points 5786.2890625 Solution is correct
#9: OK [3671ms, 38MB]: 1 50 1 82.6172 1 47.3633 1 43.1152 1 47.4487 points 5688.3666992188 Solution is correct
#10: OK [3968ms, 37MB]: 2 25.6165 1 60.0769 1 42.1265 1 45.4346 1 40.5518 points 5358.5815429688 Solution is correct
#11: OK [3625ms, 38MB]: 1 50 1 72.2135 1 38.5677 1 53.6979 1 37.3828 points 5744.4140625 Solution is correct
#12: OK [2968ms, 29MB]: 4 37.6465 4 37.7686 4 17.6514 4 16.2109 4 11.1572 points 4888.6962890625 Solution is correct
#13: OK [2843ms, 29MB]: 4 38.1836 4 48.5107 4 10.7666 4 40.2344 4 9.00879 points 4809.8876953125 Solution is correct
#14: OK [2968ms, 30MB]: 4 38.2812 4 40.6006 4 12.6221 4 18.335 4 18.5303 points 4864.892578125 Solution is correct
#15: OK [3140ms, 28MB]: 4 37.6221 4 39.2578 4 11.5234 4 4.73633 4 14.624 points 4926.708984375 Solution is correct
#16: OK [2781ms, 31MB]: 4 38.1836 4 43.8232 4 19.0918 4 18.5547 4 16.626 points 4841.162109375 Solution is correct
#17: OK [3484ms, 29MB]: 4 37.5977 4 13.9648 4 39.5264 4 16.2842 4 10.6934 points 4895.80078125 Solution is correct
#18: OK [3140ms, 29MB]: 4 37.6221 4 36.377 4 11.1816 4 3.39355 4 10.1318 points 4953.8818359375 Solution is correct
#19: OK [2640ms, 29MB]: 4 38.1592 4 37.3535 4 25.9766 4 27.0264 4 23.7793 points 4793.115234375 Solution is correct
#20: OK [3468ms, 29MB]: 8 43.75 8 42.7246 8 25 8 27.6855 8 15.7227 points 5410.3515625 Solution is correct
#21: OK [3125ms, 30MB]: 8 46.875 8 60.5957 8 29.5898 8 28.0762 8 21.0938 points 5316.30859375 Solution is correct
#22: OK [3093ms, 29MB]: 8 43.75 8 42.9199 8 24.3652 8 15.2344 8 14.3555 points 5453.125 Solution is correct
#23: OK [2781ms, 41MB]: 8 46.875 8 59.8145 8 8.88672 8 9.17969 8 41.9922 points 5374.755859375 Solution is correct
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
