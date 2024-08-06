/*
 * Proiezione voucher generati in anno, con posizione.
 *
 * Collection: Vouchers
 */
 
[
  {
    $match:
      /**
       * query: The query in MQL.
       */
      {
        $and: [
          {
            $expr: {
              $eq: [
                {
                  $year: "$timestamp"
                },
                2023
              ]
            }
          },
          {
            position: {
              $exists: true
            }
          }
        ]
      }
  },
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        _id: 1,
        count: "$initialCount",
        aimCode: 1,
        lat: {
          $last: "$position.coordinates"
        },
        lng: {
          $first: "$position.coordinates"
        }
      }
  }
]