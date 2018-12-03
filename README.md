# “Worth One Minute” platform registry

The “Worth One Minute”&nbsp;(WOM) system is an *incentives platform* designed to allow crowdsourcing/crowdsensing volunteers to be rewarded for the time they invest into positive initiatives for the social good.

This repository includes the platform’s *central registry*, that registers reward vouchers and allows payments (exchanges of vouchers for goods or services) to be made.

The platform is designed to be anonymous for volunteers.

## Installation

1. Clone the repository and ensure that Docker and Docker Compose are installed.
1. Create data volumes:  
   `make create_volumes`
1. Install database:  
   `make install`
1. Install example data (optional):  
   `make install_example`
1. Run the API server:  
   `make up`

## Documentation

Work in progress.

* [Protocol implementation details](/docs/protocols.md)

Check out our paper “[Introducing a flexible rewarding platform for mobile crowd-sensing applications](https://www.researchgate.net/publication/323868710_Introducing_a_flexible_rewarding_platform_for_mobile_crowd-sensing_applications)”, presented at CASPer’18 (PERCOM, Athens).&nbsp;📃
