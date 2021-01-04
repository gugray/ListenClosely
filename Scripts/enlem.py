import nltk
from nltk.stem import WordNetLemmatizer
from nltk.corpus import wordnet
from io import open
import codecs
import sys
import os

ep = "FAJW"

#nltk.download("wordnet")
#nltk.download("averaged_perceptron_tagger")
#nltk.download("punkt")

lemmatizer = WordNetLemmatizer()

# function to convert nltk tag to wordnet tag
def nltk_tag_to_wordnet_tag(nltk_tag):
    if nltk_tag.startswith('J'):
        return wordnet.ADJ
    elif nltk_tag.startswith('V'):
        return wordnet.VERB
    elif nltk_tag.startswith('N'):
        return wordnet.NOUN
    elif nltk_tag.startswith('R'):
        return wordnet.ADV
    else:          
        return None

def lemmatize_sentence(sentence):
    # tokenize the sentence and find the POS tag for each token
    #tok = nltk.word_tokenize(sentence)
    # actually nah, in our case, we're already getting tokenized lines
    tok = sentence.split()
    #print(tok)
    nltk_tagged = nltk.pos_tag(tok)  
    #tuple of (token, wordnet_tag)
    wordnet_tagged = map(lambda x: (x[0], nltk_tag_to_wordnet_tag(x[1])), nltk_tagged)
    lemmatized_sentence = []
    for word, tag in wordnet_tagged:
        if tag is None:
            # if there is no available tag, append the token as is
            lemmatized_sentence.append(word)
        else:        
            # else use the tag to lemmatize the token
            # lemmatize both as-is, and lower-case. if lemma is different after lower-case, we'll use that.
            # e.g., "Houses" at start of sentence.
            lem_orig = lemmatizer.lemmatize(word, tag)
            word_lo = word.lower()
            lem_lo = lemmatizer.lemmatize(word_lo, tag)
            if word_lo != word and lem_orig == word and lem_lo != word_lo:
              lemmatized_sentence.append(lem_lo)
            else:
              lemmatized_sentence.append(lem_orig)
    return " ".join(lemmatized_sentence)

#print(lemmatize_sentence("Houses are getting real expensive."))
# I be love it

with open("../_work/" + ep + "-tok.txt", "r", encoding="utf8") as f:
  with open("../_work/" + ep + "-lem.txt", "w", encoding="utf8") as g:
    for line in f:
      lemmasStr = lemmatize_sentence(line)
      g.write(lemmasStr + "\n")


