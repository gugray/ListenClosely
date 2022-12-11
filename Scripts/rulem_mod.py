from pymystem3 import Mystem
from io import open
import codecs
import sys
import os
import os.path

m = Mystem()

if len(sys.argv) < 2:
    sys.exit("Please provide the arguments [1] plain text input file path; [2] lemmetized output file path")

pathIn =  str(sys.argv[1])
pathOut =  str(sys.argv[2])

if os.path.isfile(pathIn) == False:
    sys.exit("File not found: " + os.path.abspath(pathIn))

with open(pathIn, "r", encoding="utf8") as f:
  with open(pathOut, "w", encoding="utf8") as g:
    for line in f:
      lemmas = m.lemmatize(line)
      lemmasStr = ''.join(lemmas)
      g.write(lemmasStr)


print(f"Lemmatized file " + pathOut + " saved")
sys.exit()