# Fun Puzzle: k-special hexagon
Task: https://www.topcoder.com/challenges/73c95dee-3b32-4a3a-aad6-160d68fe2013?tab=details

# 9-special hexagon
Constructing a 9-special hexagon is pretty straight-forward: assign integers to some edges and make sure that the overall hexagon remains possible and convex. Here is one of many examples how it could look like:
![9special](https://github.com/eulerscheZahl/Topcoder/blob/master/FunPuzzle01/9special.png)

Here is a bit of [sage](https://www.sagemath.org/) code to find the locations of the corners:
```
ax,ay,bx,by = 0,0,1,0
cx,cy,dx,dy,ex,ey,fx,fy = var('cx,cy,dx,dy,ex,ey,fx,fy')
eqs = [
	(cx-bx)^2 + (cy-by)^2 == 2^2,
	(dx-cx)^2 + (dy-cy)^2 == 3^2,
	(ex-dx)^2 + (ey-dy)^2 == 4^2,
	(dx-ax)^2 + (dy-ay)^2 == 5^2,
	(ex-fx)^2 + (ey-fy)^2 == 6^2,
	(ex-ax)^2 + (ey-ay)^2 == 7^2,
	(fx-ax)^2 + (fy-ay)^2 == 8^2,
	(fx-cx)^2 + (fy-cy)^2 == 9^2
]
solve(eqs, cx,cy,dx,dy,ex,ey,fx,fy)
```

This gives the following:
```
A = (0, 0)
B = (1, 0)
C = (1.47312108559499, 1.943233654333502)
D = (0.9816143497757848, 4.902696365767878)
E = (-2.704240766073871, 6.456553755522828)
F = (-7.48321554770318, 2.828689370485036)
```

# 10-special hexagon
Let's first think about how such a hexagon would look like:
It can be shown via exhaustive search, that every such hexagon with 10 integer-valued distances must also have a triangle where all sides are integers. When searching for a 10-specific hexagon programmatically it's thus a valid start to loop over all triangles in the hexagon and assign all possible tuples of integers.

As we only have 3 undefined points of the hexagon left, but still 7 sides with an integer requirement, it can be shown that there is always an undefined point which must be connected to at least two already defined points via integer lengths. Moreover this also applies for the 5th and 6th point after assigning the 4th and 5th. Thus our program can pick 2 previously constrained points as well as integer distances to compute the position of all following points.

Instead of checking for a convex hexagon, let's make sure that there are no 3 points on a straight line. This is easier to check and even a weaker constraint.
You can find a program doing exactly this [here](https://github.com/eulerscheZahl/Topcoder/blob/master/FunPuzzle01/Program.cs).
It does not find a single hexagon. However, with the arguments from above, it should find a 10-special hexagon if it exists.
Therefore the 9-special one is already optimal.

Here are some honorable mentions if we drop the convex constraint:
When we allow concave hexagons but no 3 subsequent points of the perimeter on a straight line, we can find a 10-special one (within an error margin of 1e-9).
![10concave](https://github.com/eulerscheZahl/Topcoder/blob/master/FunPuzzle01/10concave.png)

If we go so far as to allow all points on a straight line, 13-special is the highest we can get:
![13line](https://github.com/eulerscheZahl/Topcoder/blob/master/FunPuzzle01/13line.png)
