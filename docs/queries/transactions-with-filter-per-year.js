/*
 * Lista di transazioni di uno specifico anno che includono un qualsiasi filtro sui vouchers.
 *
 * Collection: PaymentRequests
 */
 
[
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        amount: 1,
        filter: 1,
        createdAt: 1,
        posId: 1,
        confirmations: {
          $filter: {
            input: "$confirmations",
            cond: {
              $eq: [
                {
                  $year: "$$this.performedAt"
                },
                2023
              ]
            }
          }
        }
      }
  },
  {
    $addFields:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        transactionCount: {
          $cond: {
            if: {
              $isArray: "$confirmations"
            },
            then: {
              $size: "$confirmations"
            },
            else: 0
          }
        }
      }
  },
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        $and: [
          {
            "filter.aims": {
              $exists: true
            }
          },
          {
            "filter.aims": {
              $ne: "0"
            }
          },
          {
            transactionCount: {
              $gt: 0
            }
          }
        ]
      }
  }
]