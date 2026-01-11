// show x and y coordinates outside of grid
	adjustFont(g, Font.SANS_SERIF, Font.PLAIN, String.valueOf("1"), new Rectangle2D.Double(0, 0, 0.2, 0.2));
	g.setColor(Color.black);
	for (int x = 0; x < N; x++) {
		drawString(g, ""+x, new Rectangle2D.Double(x + 0.5, -0.08, 0, 0));
	} 
	for (int y = 0; y < N; y++) {
		drawString(g, ""+y, new Rectangle2D.Double(-0.08, y+0.5, 0, 0));
	} 
