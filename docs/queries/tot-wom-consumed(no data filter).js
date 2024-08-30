/* Total WOM consumed (filtered for merchant if needed) */
/* Collection: PaymentRequests */
[
  {
    $match: {
      confirmations: {
        $exists: true,
        $ne: []
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
    $group: {
      _id: null,
      totalAmount: {
        $sum: {
          $multiply: [
            "$amount",
            {
              $size: "$confirmations"
            }
          ]
        }
      }
    }
  }
]
