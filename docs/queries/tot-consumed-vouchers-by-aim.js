/*
 * Totale voucher consumati ordinati per aim
 *
 * Collection: PaymentRequests
 */
[
  {
    $unwind: {
      path: "$confirmations",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: false
    }
  },
  {
    "$project": {
      "_id": 1,  // include any other top-level fields you need
      "performedAt": "$confirmations.performedAt",
      "posId": 1,
      "amount": 1,
      // Exclude the entire confirmations field if it's no longer needed
      "confirmations": 0
    }
  },
  {
    $match: {
      "performedAt": {
        $gte: ISODate("2023-04-01T00:00:00.000Z"),
        $lte: ISODate("2023-07-01T00:00:00.000Z")
      }
    }
  },
  {
    /* to filter for merchant name */
    $lookup: {
      from: "Pos",
      localField: "posId",
      foreignField: "_id",
      as: "pos",
      pipeline: [
        {
          $lookup: {
            from: "Merchants",
            localField: "merchantId",
            foreignField: "_id",
            as: "merchant"
          }
        },
        {
          $unwind: "$merchant"
        },
        {
          $match: {
            "merchant.name":
              "Cinema Teatro Ducale"
          }
        }
      ]
    }
  },
  {
    /* to filter for merchant name */
    $unwind: {
      path: "$pos",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: false
    }
  },
  {
    $group: {
      _id: {
        aim: {
          $ifNull: ["$filter.aims", "NoAim"]
        }
      },
      totalAmount: {
        $sum: "$amount"
      }
    }
  },
  {
    $project: {
      _id: 0,
      aimCode: "$_id.aim",
      totalAmount: 1
    }
  },
  {
    $sort: {
      totalAmount: -1
    }
  }
]
