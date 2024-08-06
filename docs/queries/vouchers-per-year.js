/*
 * Voucher generati e riscossi in uno specifico anno da un Instrument.
 *
 * Collection: GenerationRequests
 */

[
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        sourceId: ObjectId(
          "5e737fba9036edb02d61ef6d"
        ),
        isVerified: true,
        $expr: {
          $eq: [
            {
              $year: "$createdAt"
            },
            2020
          ]
        }
      }
  },
  {
    $group:
      /**
       * _id: The id of the group.
       * fieldN: The first field name.
       */
      {
        _id: 1,
        totalCount: {
          $sum: "$totalVoucherCount"
        },
        redeemedCount: {
          $sum: {
            $cond: {
              if: "$performedAt",
              then: "$totalVoucherCount",
              else: 0
            }
          }
        }
      }
  }
]