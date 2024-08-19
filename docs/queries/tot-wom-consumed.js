/*
 * Total numero dei voucher consumati in un intervallo di tempo.
 *
 * Collection: PaymentRequests
 */

[
  {
    $match:
    /**
     * query: The query in MQL.
     */
      {
        createdAt: {
          $gte: ISODate(
            "2022-07-01T00:00:00.000Z"
          ),
          $lte: ISODate(
            "2023-07-01T00:00:00.000Z"
          )
        }
      }
  },
  {
    $group: {
      _id: {
        $dateToString: {
          format: "%Y-%m-%d",
          date: "$createdAt"
        }
      },
      totalAmount: {
        $sum: {
          $multiply: [
            "$amount",
            {
              $cond: {
                if: {
                  $isArray: "$confirmations" // Check if confirmations is an array
                },
                then: {
                  $size: "$confirmations" // If it is, use its size
                },
                else: 0 // If it's not present, use 0
              }
            }
          ]
        }
      }
    }
  },
  {
    $sort:
    /**
     * Provide any number of field/order pairs.
     */
      {
        _id: -1
      }
  }
]
