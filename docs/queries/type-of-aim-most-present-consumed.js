/*
 * Totale voucher consumati ordinati per aim
 *
 * Collection: PaymentRequests
 */
[
  {
    $match: {
      createdAt: {
        $gte: ISODate("2022-07-01T00:00:00.000Z"),
        $lte: ISODate("2023-07-01T00:00:00.000Z")
      }
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
