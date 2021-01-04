from germalemma import GermaLemma
from io import open
import codecs
import sys
import os

ep = "MERCOR"

lemmatizer = GermaLemma()

with open("../_work/" + ep + "-surfs.txt", "r", encoding="utf8") as f:
  with open("../_work/" + ep + "-v-lems.txt", "w", encoding="utf8") as g:
    for line in f:
      print(line.strip())
      lemma = lemmatizer.find_lemma(line.strip(), 'V')
      g.write(lemma + "\n")

