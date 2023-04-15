import os, sys
import json, json.tool

# install dependency with:
# pip install tk
from tkinter.filedialog import askopenfilename
from tkinter.messagebox import showerror
from tkinter.messagebox import askyesno

# The script purposed for fix oldest data generated by the program versions 
# before implementation of sorting of translations by language
# It reads a *-segs.json file, sorts the data and writes into a new fixed file.

# Read Json data fron the input file
def readJson(inputFileP):
    if not os.path.isfile(inputFileP):
        raise Exception("Cannot read %s" % inputFileP)

    inputFile = open(inputFileP, "r", encoding="utf-8-sig")
    if not inputFile.readable():
        raise Exception("Cannot read %s" % inputFileP)

    content = inputFile.read()
    try:
        jsonData = json.loads(content)
    except Exception as e:
        raise Exception("Cannot read %s.\nFile is corrupt (no JSON content)" % inputFileP)

    return jsonData

# Find the langMark in the string, like '[de]', '[fr] ...
def getLngMark(srcDef):
    # default (no language mark)
    ret = "[--]"
    if srcDef.startswith("["):
        ret = srcDef[0:4]
    return ret

# Help function: 
# for the lngMark, find the index position in idx['idx']
# Append all entries found at this position in idx['vals'] into the given list valsNew
def appendForLngMark(lngMark, idx, valsNew):
    if lngMark in idx['idx']:
        pos = idx['idx'].index(lngMark)
        for val in idx['vals'][pos]:
            valsNew.append(val)

# Resort the given Json dict senses for have first entries entries without language, 
# then with language [de], then entries sorted by language alphabetically
# The input structure will be overwritten.
def resortSenses(senses):
    idx = {
        "idx" : [],
        "vals" : []
    }
    
    for sense in senses:
        srcDef = sense['srcDef']
        lngMark = getLngMark(srcDef)
        vals = list()
        if lngMark in idx['idx']:
            pos = idx['idx'].index(lngMark)
            vals = idx['vals'][pos]
        else:
            idx['idx'].append(lngMark)
            idx['vals'].append(vals)
        vals.append(srcDef)
        
    valsNew = list()
    # entries without lang
    appendForLngMark("[--]", idx, valsNew)
    # entries with DE lang
    appendForLngMark("[de]", idx, valsNew)
    # other entries: sorted alphabetically
    idxCopy = idx['idx'].copy()
    idxCopy.sort()
    for lngMark in idxCopy:
        if(lngMark != "[de]" and lngMark != "[--]"):
            appendForLngMark(lngMark, idx, valsNew)

    # overwrite the provided data by sorted entries
    senses.clear()
    for vn in valsNew:
        sense = {
            'srcDef': vn
        }
        senses.append(sense)

# Overwrite the jsonData structure by sort the 'senses' member for each entry in jsonData['dictEntries']
def resortDictEntries(jsonData):

    dic = None
    try:
        dic = jsonData['dictEntries']
    except Exception as e:   
        raise Exception("Cannot read JSON data.\nData is corrupt (no dictEntries element)")

    for dicEntry in dic:
        if dicEntry['senses']:
            resortSenses(dicEntry['senses'])

# Store the output file
def storeModFile(jsonData, outputFileP):
    outputFile = None
    try:
        outputFile = open(outputFileP, "w", encoding="utf8")
    except Exception as e:
        raise Exception("Cannot write into %s\nCannot create file" % outputFileP)
        
    if not outputFile.writable():
        raise Exception("Cannot write into %s\nWrite denied" % outputFileP)
    
    try:
        json.dump(jsonData, outputFile, ensure_ascii=False, indent=2)
    except Exception as e:
        raise Exception("Cannot write into %s\nCorrupt JSON data" % outputFileP)


def selectInputFile():
    ret = None
    
    filename = askopenfilename(
        initialdir='..',
        multiple='False',
        title='Please select a JSON file',
        filetypes=[("*-segs.json", ".json")]
        )
    # file is selected
    if filename: 
        ret = filename

    return ret


if __name__ == '__main__':
    
    while True:
        inputFileP = selectInputFile()
        # Cancelled
        if inputFileP == None:
            exit(0)

        inputFileP  = os.path.abspath(inputFileP)
        
        if not inputFileP.lower().endswith("-segs.json"):
            showerror(title="Error", message="Error by open JSOIN file:\nThe file name MUST be *-segs.json")
            exit(1)

        outputFileP = os.path.abspath(inputFileP + ".mod")
        
        try :
            jsonData = readJson(inputFileP)
        except Exception as e:
            showerror(title="Error", message=("Error by open JSOIN file:\n%s" % str(e)) )
            exit(1)
            
        try :
            resortDictEntries(jsonData)
        except Exception as e:
            showerror(title="Error", message=("Error by read JSOIN data:\n%s" % str(e)))
            exit(1)
            
        try :
            storeModFile(jsonData, outputFileP)
        except Exception as e:
            showerror(title="Error", message=("Error by store the JSOIN file:\n%s" % str(e)))
            exit(1)

        if not askyesno(title="Done", message="File %s created. Do you wish to process next file?" % outputFileP):
            exit(0)