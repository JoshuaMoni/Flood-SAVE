import json
import numpy as np
import matplotlib.pyplot as plt


def convert_coords(location):
    print(f"Loading data for {location}")
    with open(f"data/{location}_all.json", "r") as d_file:
        ll_data = json.load(d_file)
    ll_min_x = min(ll_data["terrainData"], key=lambda x: x['X'])['X']
    ll_max_x = max(ll_data["terrainData"], key=lambda x: x['X'])['X']
    ll_min_y = min(ll_data["terrainData"], key=lambda x: x['Y'])['Y']
    ll_max_y = max(ll_data["terrainData"], key=lambda x: x['Y'])['Y']

    t_data = {"orakei": [3521, 4074], "cbd": [3376, 3796], "penrose": [5310, 2584]}
    t_min_x = 0
    t_max_x = t_data[location][0]
    t_min_y = 0
    t_max_y = t_data[location][1]

    print("Calculating equations")
    scale_factor_x = np.polyfit((ll_min_x, ll_max_x), (t_max_x, t_min_x), 1)
    scale_factor_y = np.polyfit((ll_min_y, ll_max_y), (t_max_y, t_min_y), 1)
    print(f"X equation: x2 = {round(scale_factor_x[0], 2)}x2 + {round(scale_factor_x[1], 2)}")
    print(f"Y equation: y2 = {round(scale_factor_y[0], 2)}y2 + {round(scale_factor_y[1], 2)}")
    im_array = []

    print("Altering data")
    for i, item in enumerate(ll_data["terrainData"]):
        old_x = ll_data["terrainData"][i]["X"]
        old_y = ll_data["terrainData"][i]["Y"]
        ll_data["terrainData"][i]["X"] = old_x * scale_factor_x[0] + scale_factor_x[1]
        ll_data["terrainData"][i]["Y"] = old_y * scale_factor_y[0] + scale_factor_y[1]
        if ll_data["terrainData"][i]["type"] == "water":
            im_array.append([ll_data["terrainData"][i]["X"], ll_data["terrainData"][i]["Y"]])

    print("Saving verify image")
    im_array = np.array(im_array)
    plt.scatter(x=im_array.T[0], y=im_array.T[1], s=0.1, color='blue')
    plt.xlim(t_min_x, t_max_x)
    plt.ylim(t_min_y, t_max_y)
    plt.savefig(f"data/{location}_verify.png")
    plt.clf()

    print("Saving altered data\n")
    with open(f"data/{location}_unity.json", "w") as format_file:
        json.dump(ll_data, format_file)


def main():
    convert_coords("cbd")
    convert_coords("orakei")
    convert_coords("penrose")


if __name__ == "__main__":
    main()
