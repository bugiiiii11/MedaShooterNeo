# Usage:
# python frames_optimizer.py -i "john" -o "john_output" -s 5 -d 3 -n "john"

import sys
import os
from PIL import Image
import math

inputDir = ""
outputDir = ""
stepSize = 0
quality = 20
dimensions = 2
newName = "output"

def closestNumber(n, m) :
    # Find the quotient
    q = int(n / m)
     
    # 1st possible closest number
    n1 = m * q
     
    # 2nd possible closest number
    if((n * m) > 0) :
        n2 = (m * (q + 1))
    else :
        n2 = (m * (q - 1))
     
    # if true, then n1 is the required closest number
    if (abs(n - n1) < abs(n - n2)) :
        return n1
     
    # else n2 is the required closest number
    return n2

def runConversion():
    filenames = next(os.walk(inputDir), (None, None, []))[2]  # [] if no file
    currentSkip = 0
    
    isExist = os.path.exists(outputDir)
    
    allImages = math.ceil(len(filenames) / stepSize)
    currentProgress = 1
    
    if not isExist:
        os.makedirs(outputDir)
    
    for name in filenames:

        if currentSkip == 0:
            path = f"{inputDir}/{name}"
            im = Image.open(path)
            
            width,height = im.size
            newSize = (closestNumber(int(width/dimensions), 4), closestNumber(int(height/dimensions),4))
           
            im = im.resize(newSize)
            
            im.save(f"{outputDir}/{newName}_{currentProgress}.png", quality=quality)
            
            print("saved to", f"{outputDir}/{name}", f"[size={newSize}, quality={quality}]", f"{currentProgress}/{allImages}")
            currentProgress += 1
        currentSkip += 1
        
        if(currentSkip == stepSize):
            currentSkip = 0
    
n = len(sys.argv)
if n < 6:
    print("required params are --input, --output and --step-size");
    exit(1)

for i in range(1, n):
    arg = sys.argv[i]
    if (arg == "-i" or arg == "--input"):
        inputDir = sys.argv[i+1]
        i+=1
    
    if (arg == "-o" or arg == "--output"):
        outputDir = sys.argv[i+1]
        i+=1
        
    if (arg == "-s" or arg == "--step-size"):
        stepSize = int(sys.argv[i+1])
        i+=1
        
    if (arg == "-d" or arg == "--dimensions"):
        dimensions = float(sys.argv[i+1])
        i+=1
    
    if (arg == "-n" or arg == "--new-name"):
        newName = sys.argv[i+1]
        i+=1
        
if inputDir != "" and outputDir != "" and stepSize > 0:
    runConversion()