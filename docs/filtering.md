# Voucher filtering

This is work in progress.
Final filtering proposal will be updated as development proceeds.

When creating a new payment, a filter *can* be specified.
Vouchers spent for the payment *must* satisfy the filter's criteria in order to be accepted.
Filtering is performed both on the client&nbsp;(Pocket) and on the server&nbsp;(Registry).

## Filter types

### Simple filters

For basic functionality, a simple filter mode can be used.
A simple filter has several limitations, that make its implementations more performant and its specification easier.

* A single, optional, filter on the voucher's source/aim;
* A single, optional, condition on the voucher's generation timestamp, expressed as a simple “maximum age”;
* A single, optional, geographical filter expressed as a bounding box.

As a JSON object, a simple filter takes the following form:

```json
{
    "Type": "simple",
    "Source": "URL",
    "Age": 12345,
    "BoundingBox": {
        "NorthEast": {
            "type": "Point",
            "coordinates": [ 43.732383, 12.646840 ]
        },
        "SouthWest": {
            "type": "Point",
            "coordinates": [ 43.717436, 12.620702 ]
        }
    }
}
```

Where:
* `Source` is the URL to the voucher's generator (see *Source/Aim filtering* below for further details);
* `Age` is the amount of seconds back in time (since UTC ‘now’) that is accepted;
* `BoundingBox` is a basic axis-aligned bounding box specified by two points. Each point is expressed as a [GeoJSON](https://tools.ietf.org/html/rfc7946) geometry that *must* be of type `Point`.

A filter *must* have at least one filtering condition.

### Complex filters

Complex filters can be used to express advanced filtering options using logical operators.
They are encoded as a hierarchy of JSON objects, in the following form:

```json
{
    "Type": "complex",

    "Source": "URL",
    "TimeRange": [ "2018-01-01", "2018-01-31" ],
    "Age": 12345,
    "BoundingBox" : { },
    "Geometry": {
        "type": "Polygon",
        "coordinates": [
            [ 43.732383, 12.646840 ],
            [ 43.717436, 12.620702 ],
            [ 43.732383, 12.620702 ]
        ]
    },

    "Or": [ ]
}
```

Where:
* `Source`, `Age`, and `BoundingBox` work just like in simple filters;
* `TimeRange` expresses an interval of time, encoded as a couple of ISO&nbsp;dates;
* `Geometry` can take any [GeoJSON](https://tools.ietf.org/html/rfc7946) geometry object (but only `Polygon` types make sense and are supported by default);
* `Age` and `TimeRange`, `BoundingBox` and `Geometry` are mutually exclusive.

Any filter *may* have *any number* of the filtering conditions above.
A filter without any filtering condition is valid and accepts all vouchers.

The `Or` property takes an array of filter objects (which must also be of the “complex” type).
The parent filter accepts a voucher if *any* sub-filter accepts the voucher.

For example, the following filter accepts all vouchers generated by a specific source around Urbino (`Source` and `BoundingBox` filters) or any voucher generated during the last week (`Age` filter).

```json
{
    "Type": "complex",
    "Or": [
        {
            "Type": "complex",
            "Source": "https://wom.social/...",
            "BoundingBox": {
                "NorthEast": {
                    "type": "Point",
                    "coordinates": [ 43.732383, 12.646840 ]
                },
                "SouthWest": {
                    "type": "Point",
                    "coordinates": [ 43.717436, 12.620702 ]
                }
            }
        },
        {
            "Type": "complex",
            "Age": 604800
        }
    ]
}
```

## Source/Aim filtering

TBD.
