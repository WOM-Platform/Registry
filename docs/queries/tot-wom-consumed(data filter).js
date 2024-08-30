/* Total WOM consumed (filtered for merchant if needed) */
/* Collection: PaymentRequests */
[
  {
    $unwind: {
      path: "$confirmations",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: false
    }
  },
  {
    $match: {
      "confirmations.performedAt": {
        $gte: ISODate("2023-01-03"),
        $lte: ISODate("2024-04-03")
      }
    }
  },
  {
    /* to filter for merchant */
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
            "merchant.name": "Demo Merchant"
          }
        }
      ]
    }
  },
  {
    /* to filter for merchant */
    $unwind: {
      path: "$pos",
      includeArrayIndex: "string",
      preserveNullAndEmptyArrays: false
    }
  },
  {
    $group:
    /**
     * _id: The id of the group.
     * fieldN: The first field name.
     */
      {
        _id: null,
        totalAmount: {
          $sum: "$amount"
        }
      }
  }
]
