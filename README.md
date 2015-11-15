# Neptune's Pride State Downloader

Takes a set of Neptunes Pride (http://np.ironhelmet.com/) dumps produced by
https://github.com/malorisdead/NeptunesPrideStateDownloader and renders a map
for each turn.  Performs rudimentary conflict analysis, in case anyone tries to
munge the data.

This is intended for strategic sharing of information between allies, and for
a recap of the game after it's concluded.

##Setup
1. Edit App.config and enter the directory all of the dumps were saved in.
2. Run!
3. Each turn of the game will produce a separate SVG map of the collective
   state of the world during that turn, according to anyone who contributed
   their data dumps.
