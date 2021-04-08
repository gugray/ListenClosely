from pymystem3 import Mystem
from io import open
import codecs
import sys
import os
import os.path

m = Mystem()

if len(sys.argv) < 2:
    sys.exit("Please provide the name")

ep = str(sys.argv[1])
path =  "../_work/" + ep + "-plain.txt"

if os.path.isfile(path) == False:
    sys.exit("File not found: " + os.path.abspath(path))

with open(path, "r", encoding="utf8") as f:
  with open("../_work/" + ep + "-lem.txt", "w", encoding="utf8") as g:
    for line in f:
      lemmas = m.lemmatize(line)
      lemmasStr = ''.join(lemmas)
      g.write(lemmasStr)

