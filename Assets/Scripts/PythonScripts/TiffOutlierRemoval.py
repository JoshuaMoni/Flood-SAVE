from statistics import mean, median
from PIL import Image
import numpy as np
from scipy import ndimage
from matplotlib import pyplot as plt


"""This file processes the terrain data tif files and removes outliers"""

SIGMA = 25
UPPER_BOUND = 60
DELTA_PERCENT = 200
ITERATIONS = 5

found_file = False
file_string = ""
while not found_file:
    file_string = input("Enter region name: ")
    try:
        im = Image.open("data/{}.tif".format(file_string))
        found_file = True
        if file_string == "orakei":
            UPPER_BOUND = 70
        if file_string == "sylvia":
            UPPER_BOUND = 55
    except FileNotFoundError:
        pass

imarray = np.array(im)
"""max_pixel = int(np.amax(np.rint(imarray)))
plt.hist(imarray.flatten(), bins=list(range(0, 100)))
plt.show()
plt.clf()"""
imOutput = im
for i in range(ITERATIONS):
    print("Iteration {} out of {}".format(i+1, ITERATIONS))
    # mean_image = ndimage.convolve(imarray, np.full((SIGMA, SIGMA), 1/(SIGMA**2)), mode='reflect')
    median_image_1 = ndimage.median_filter(imarray, 10)
    median_image_2 = ndimage.median_filter(imarray, 5)
    min_image = ndimage.minimum_filter(imarray, 25)

    outputarray = imarray.copy()

    for row_number in range(len(imarray)):
        for column_number in range(len(imarray[0])):
            pixel = imarray[row_number][column_number]

            if min_image[row_number][column_number] < 2 and pixel > UPPER_BOUND:
                outputarray[row_number][column_number] = min_image[row_number][column_number]

            """if pixel > (median_image_1[row_number][column_number] * (DELTA_PERCENT / 100)):
                outputarray[row_number][column_number] = median_image_2[row_number][column_number]"""
            if min_image[row_number][column_number] > 2 and pixel > UPPER_BOUND:
                outputarray[row_number][column_number] = median_image_2[row_number][column_number]
    imOutput = Image.fromarray(outputarray)
    imarray = outputarray
    imOutput.save("data/{}pass{}.tif".format(file_string, i+1))
