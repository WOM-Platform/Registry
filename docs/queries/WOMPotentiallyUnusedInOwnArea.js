/*
 * WOM Potenzialmente non utilizzati nella propria area
 *
 * Collection: Vouchers
 */
[
  {
    $geoNear: {
      near: {
        type: "Point",
        coordinates: [12.636, 43.726]
      },
      distanceField: "distance",
      maxDistance: 50 * 100,
      spherical: true
    }
  },
  {
    $match: {
      $or: [
        {
          count: {
            $exists: false
          }
        },
        {
          count: {
            $gt: 0
          }
        }
      ]
    }
  },
  {
    $group: {
      _id: null,
      totalUnusedVouchers: {
        $sum: "$count"
      }
    }
  },
  {
    $project: {
      _id: 0,
      totalUnusedVouchers: 1
    }
  }
]
