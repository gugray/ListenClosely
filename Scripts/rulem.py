from pymystem3 import Mystem
from io import open
import codecs
import sys
import os

m = Mystem()

ep = "LTL_VIM"

with open("../_work/" + ep + "-plain.txt", "r", encoding="utf8") as f:
  with open("../_work/" + ep + "-lem.txt", "w", encoding="utf8") as g:
    for line in f:
      lemmas = m.lemmatize(line)
      lemmasStr = ''.join(lemmas)
      g.write(lemmasStr)

