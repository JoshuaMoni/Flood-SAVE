import json
import numpy as np
from datetime import datetime, timedelta
import matplotlib.pyplot as plt


def json_opener(path):
    """Open json file from path and return dictionary object"""
    with open(f"data/{path}.json", "r") as w_file:
        data = json.load(w_file)
    return data


def main():
    """Convert the weather data into a form that will work best in Unity"""
    h_data = json_opener("akl")
    h_rain = np.array([h_data['hourly']['time'], h_data['hourly']['precipitation']]).transpose()
    for i in range(len(h_rain)):
        h_rain[i][1] = h_rain[i][1] if h_rain[i][1] is not None else 0
    max_hi = np.argmax(h_rain, axis=0)[1]
    print(h_rain[max_hi-10:max_hi+10])
    max_h = h_rain[max_hi]
    print(f"Max hourly rain: {max_h[0]} {datetime.fromtimestamp(max_h[0])}, {max_h[1]}mm")
    x = [datetime.fromtimestamp(0) + timedelta(seconds=x) for x in h_rain[max_hi-500:max_hi+500, 0]]
    y = h_rain[max_hi-500:max_hi+500, 1]
    plt.figure()
    plt.plot(x, y)
    plt.xticks(rotation=0)
    plt.ylabel("Rainfall in millimeters")
    plt.title("Auckland Rainfall")
    plt.tight_layout()
    plt.savefig("images/rainfall.png")

    data = {"weatherData": [{"time": x[0], "rainfall": x[1]} for x in h_rain]}
    with open("data/akl_processed.json", "w") as new_file:
        json.dump(data, new_file)
    pass


if __name__ == "__main__":
    main()
