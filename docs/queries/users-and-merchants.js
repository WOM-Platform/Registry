/*
 * Lista utenti e relativi merchant gestiti.
 *
 * Collection: Users
 */

[
  {
    $lookup:
      /**
       * from: The target collection.
       * localField: The local join field.
       * foreignField: The target join field.
       * as: The name for the results.
       * pipeline: Optional pipeline to run on the foreign collection.
       * let: Optional variables to use in the pipeline field stages.
       */
      {
        from: "Merchants",
        localField: "_id",
        foreignField: "access.userId",
        as: "merchants"
      }
  },
  // {
  //   $match:
  //     /**
  //      * query: The query in MQL.
  //      */
  //     {
  //       verificationToken: {
  //         $exists: 1
  //       }
  //     }
  // }
  {
    $project:
      /**
       * specifications: The fields to
       *   include or exclude.
       */
      {
        email: 1,
        name: 1,
        surname: 1,
        role: 1,
        verificationToken: 1,
        merchantCount: {
          $size: "$merchants"
        },
        merchants: 1
      }
  }
]