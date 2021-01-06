from pymystem3 import Mystem
from io import open
import codecs
import sys
import os

text = "Красивая мама красиво мыла раму"
text = "Флами́нговые (лат. Phoenicopteridae) — семейство новонёбных птиц отряда фламингообразных. Крупные птицы с очень длинной шеей и ногами."
text = "Наташа удержалась от своего первого движения выбежать к ней и осталась в своей засаде, как под шапкой-невидимкой, высматривая, чтò делалось на свете."

m = Mystem()

ep = "SAMPLE"

with open("../_work/" + ep + "-plain.txt", "r", encoding="utf8") as f:
  with open("../_work/" + ep + "-lem.txt", "w", encoding="utf8") as g:
    for line in f:
      lemmas = m.lemmatize(line)
      lemmasStr = ''.join(lemmas)
      g.write(lemmasStr)

