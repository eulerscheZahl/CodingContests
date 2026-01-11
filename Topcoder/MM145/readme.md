## Strategy
Initially I assign a movement time of 0 to all lifts. Then I generate actions (see below) that get a score penalty based on the time it takes to complete the task (+ time used by the lift with previous moves) and also give a bonus for dropping someone off (2 turns per person) or letting someone enter the lift (1 turn per person). Each action also gets a random penalty (0-5 turns) to add a bit of variation.
I then take the action with the smallest penalty and apply it. Repeat the process as long as there are valid actions, i.e. people to transport.

I store the output of the turn (the first action of each lift) along with a total penalty equal to the expected waiting time. This process is repeated 20 times and the smallest waiting time for each output is kept. Let's call these 20 iterations a sub-turn.

To deal with the uncertainty of target floors, I simply randomize the targets. Repeat the process for 10ms: randomize targets, run a sub-turn on those. Depending on the testcase I can realistically do somewhere between 6 and 50 sub-turns per turn. Using more time offline has no notable effect on the score (which is not at all what I would expect).

I then combine the results of the sub-turns by computing the average waiting time for each possible output. The action leading to the smallest expected penalty gets printed. This works surprisingly well even for 5 lifts, where it's not at all guaranteed that all sub-turns even produce the same first actions.

If a lift has nothing to do, it gets sent to the floor with the highest spawn rate and waits there with the doors open.

## Move generation
For a given lift, I generate the following possible actions:
* open the door at the current floor to let someone in or drop someone off
* move to a floor where the lift can drop off a person
* stop at an intermediate floor in direction of a possible drop-off to let someone in
* deviate by 1 floor to let someone in, even if there is no drop-off in that direction

## Scores
As achieved on TC servers by my last submit
```
Test Case  #1: Score =  4.5142
Test Case  #2: Score = 17.1763
Test Case  #3: Score = 18.8815
Test Case  #4: Score =  3.5242
Test Case  #5: Score =  7.5146
Test Case  #6: Score =  9.0761
Test Case  #7: Score =  9.9936
Test Case  #8: Score =  7.2266
Test Case  #9: Score = 39.2766
Test Case #10: Score = 28.7514
```
As the leaderboard is broken: when I compare my scores against the starter code on seed 1-1000, the starter drops down to 52.670 (with my own code reaching a perfect 100).