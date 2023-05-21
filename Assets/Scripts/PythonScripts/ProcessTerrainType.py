import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import json


def get_drains(location):
    """Get drain data given a location"""
    catch = pd.read_csv(f"data/{location}data/{location}_catch.csv")
    io = pd.read_csv(f"data/{location}data/{location}_in_out.csv")
    m = pd.read_csv(f"data/{location}data/{location}_manhole.csv")
    return catch, io, m


def get_dan_soil(location):
    """Get the satellite soil data given a location"""
    return pd.read_csv(f"data/{location}data/10DayNVDIMedian{location}.csv")


def get_minx_data(location):
    """Get the maps soil and water data given a location"""
    grass = pd.read_csv(f"data/{location}data/{location}2_g.txt")
    water = pd.read_csv(f"data/{location}data/{location}2_b.txt")
    other = pd.read_csv(f"data/{location}data/{location}2_other.txt")
    return grass, water, other


def show_save_map(location, d0, d1, dc, dio, dm, grass, water, other, show=False):
    """Given a location and data, show and save an image of the data"""
    print(f"Creating image for {location}")
    plt.clf()
    fig = plt.figure()
    fig.set_size_inches(12, 10)
    plt.scatter(x=d0['X'], y=d0['Y'], color='grey')
    plt.scatter(x=d1['X'], y=d1['Y'], color='darkgreen', s=0.8)
    plt.scatter(x=dc['X'], y=dc['Y'], color='red', s=1, label='Catchment Pits')
    plt.scatter(x=dio['X'], y=dio['Y'], color='orange', s=1, label='Inlet / Outlet')
    plt.scatter(x=dm['X'], y=dm['Y'], color='pink', s=1, label='Manholes')
    plt.scatter(x=grass['X'], y=grass['Y'], color='darkgreen', s=0.01, label='Grass')
    plt.scatter(x=water['X'], y=water['Y'], color='darkblue', s=0.01, label='Water')
    plt.rc('xtick', labelsize=18)
    plt.rc('ytick', labelsize=18)
    # plt.scatter(x=other['X'], y=other['Y'], color='slategrey', s=0.5, label='Other')
    plt.title(location.upper(), size=50)
    legend = plt.legend(loc='upper right')
    legend.legendHandles[0]._sizes = [30]
    legend.legendHandles[1]._sizes = [30]
    legend.legendHandles[2]._sizes = [30]
    legend.legendHandles[3]._sizes = [30]
    legend.legendHandles[4]._sizes = [30]
    plt.rc('legend', fontsize=18)
    plt.rc('figure', titlesize=22)
    if show:
        plt.show()
    plt.savefig(f"images/{location}_data.png")
    plt.clf()


def get_min(arrays, dim):
    """Gets the minimum value on a certain dimension from a list of same-shape arrays"""
    min_values = []
    for array in arrays:
        min_values.append(array[dim].min())
    return min(min_values)


def get_max(arrays, dim):
    """Gets the maximum value on a certain dimension from a list of same-shape arrays"""
    max_values = []
    for array in arrays:
        max_values.append(array[dim].max())
    return max(max_values)


def scale_minx_data(latlng, minx):
    """Scales dataset minx to be the same scale as latlng"""
    min_x = get_min(latlng, 'X')
    min_y = get_min(latlng, 'Y')
    max_x = get_max(latlng, 'X')
    max_y = get_max(latlng, 'Y')

    min_x2 = get_min(minx, 'X')
    min_y2 = get_min(minx, 'Y')
    max_x2 = get_max(minx, 'X')
    max_y2 = get_max(minx, 'Y')
    scale_factor_x = np.polyfit((min_x2, max_x2), (min_x, max_x), 1)
    scale_factor_y = np.polyfit((min_y2, max_y2), (max_y, min_y), 1)
    for array in minx:
        array['Y'] = array['Y'].apply(lambda value: (value * scale_factor_y[0]) + scale_factor_y[1])
        array['X'] = array['X'].apply(lambda value: (value * scale_factor_x[0]) + scale_factor_x[1])
    return minx


def save_arrays(arrays, tags, path):
    """Saves the given arrays to the path as a json file"""
    print(f"Saving arrays to: {path}")

    for i, array in enumerate(arrays):
        array.drop(columns=array.columns.difference(['X', 'Y']), axis=1, inplace=True)
        array['type'] = tags[i]

    data = pd.concat(arrays)
    data.reset_index(inplace=True)
    data.drop(columns="index", axis=1, inplace=True)

    with open(path, 'w') as data_file:
        json.dump({"terrainData": data.to_dict(orient="records")}, data_file, indent=4)


def main():
    print("Importing drains")
    cbd_c, cbd_io, cbd_m = get_drains("cbd")
    orakei_c, orakei_io, orakei_m = get_drains("orakei")
    penrose_c, penrose_io, penrose_m = get_drains("penrose")

    print("Importing soil")
    cbd_base = get_dan_soil("cbd")
    orakei_base = get_dan_soil("orakei")
    penrose_base = get_dan_soil("penrose")

    print("Importing grass, water and other")
    cbd_g, cbd_b, cbd_o = get_minx_data("cbd")
    orakei_g, orakei_b, orakei_o = get_minx_data("orakei")
    penrose_g, penrose_b, penrose_o = get_minx_data("penrose")

    mask = cbd_base['value'] == 0
    cbd_0 = cbd_base[mask]
    cbd_1 = cbd_base[~mask]
    mask = orakei_base['value'] == 0
    orakei_0 = orakei_base[mask]
    orakei_1 = orakei_base[~mask]
    mask = penrose_base['value'] == 0
    p_0 = penrose_base[mask]
    p_1 = penrose_base[~mask]

    cbd_g, cbd_b, cbd_o = scale_minx_data([cbd_c, cbd_io, cbd_m, cbd_0, cbd_1], [cbd_g, cbd_b, cbd_o])
    orakei_g, orakei_b, orakei_o = scale_minx_data([orakei_c, orakei_io, orakei_m, orakei_0, orakei_1], [orakei_g, orakei_b, orakei_o])
    penrose_g, penrose_b, penrose_o = scale_minx_data([penrose_c, penrose_io, penrose_m, p_0, p_1], [penrose_g, penrose_b, penrose_o])

    show_save_map("cbd", cbd_0, cbd_1, cbd_c, cbd_io, cbd_m, cbd_g, cbd_b, cbd_o)
    show_save_map("orakei", orakei_0, orakei_1, orakei_c, orakei_io, orakei_m, orakei_g, orakei_b, orakei_o)
    show_save_map("penrose", p_0, p_1, penrose_c, penrose_io, penrose_m, penrose_g, penrose_b, penrose_o)

    tags = ["greenery", "catch", "inout", "manhole", "grass", "water"]
    save_arrays([cbd_1, cbd_c, cbd_io, cbd_m, cbd_g, cbd_b], tags, "data/cbd_all.json")
    save_arrays([orakei_1, orakei_c, orakei_io, orakei_m, orakei_g, orakei_b], tags, "data/orakei_all.json")
    save_arrays([p_1, penrose_c, penrose_io, penrose_m, penrose_g, penrose_b], tags, "data/penrose_all.json")
    print("Done")


if __name__ == "__main__":
    main()
