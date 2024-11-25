# DPC Development Notes
Additional Contributors are welcome! 

Please take a look through the following information to familiarise yourself with the development processes and approaches to design. Please reach out to a core maintainer and/or create an issue before starting development on anything significant to ensure that there is a high probability of merge.

Join the development chat at https://discord.gg/5wWrUVVuHS

# Environment Setup

- Visual Studio Community Edition 2022 or Better https://visualstudio.microsoft.com/vs/community/
- The following features
    - .Net Desktop Development
    - .Net Framework 4.6.2 Targeting Pack
- Required Extensions
    - PowerShell Tools for Visual Studio 2022
    - HeatWave for VS2022 (Wix Installer)
- Recommended Extensions
    - Trailing Whitespace Visualizer
    - Visual Studio Spell Checker

# Design Philosophy
DPC was initially created with the following philosophy. Trying to keep these points in mind (unless there is a good, documented reason) will increase the probability of changes being accepted. They are not hard and fast rules but have successfully minimised client deployment issues in the past.

- Make use of integrated Windows components and systems where possible
- DPC is a AOVPN profile management utility and shouldn't stray outside this core focus
- User input should be validated where possible and any scenarios which we preemptively know will cause a VPN Profile to fail should block a profile update and raise an error message
- Event Logs (ETW) enables a consistent and admin friendly way to troubleshoot issues
- DPC provides a opinionated approach to AOVPN profiles, encouraging and mandating minimum security standards where possible
- DPC should never require security of a AOVPN connection to be downgraded to operate with DPC
- DPC is deployed on over 100,000 client devices each one of these devices is potentially a ruined day if DPC goes wrong and breaks the VPN tunnel. Bugs, when triggered by a global rollout, can cause BC/DR and will cause significant pain with real world costs and impact. As such always be mindful of the administrators and end users when developing and testing code
- DPC was developed in the UK as such British spelling will be used where applicable :wink:
- Client support should be maximised to ensure the greatest possible value to end users and administrators. As such .Net Framework 4.6.2 is currently used as it is the oldest supported version of .Net and therefore is pre-installed all supported Windows 10 and 11 systems.

# Localisation
While we would welcome the localisation of Events and Group Policy settings, unfortunately the core developers (and most users) are English speakers only and as such settings must be English first with localisation as an optional extra. If someone would like to step up as a localisation lead please do 

# Dependencies
DPC tries to minimize the use of dependencies from third parties to minimise attack surface and avoid licensing issues. Where dependencies are used they should always be kept up-to-date to minimise security issues

# Testing

- While there isn't 100% automated test code coverage, all automated tests must pass before a new release is finalized
- Any large or significant changes should be tested in a live environment prior to being released
- A couple of tests are OS specific use the filter -trait:Windows10 to exclude Windows 10 tests when running the tests on Windows 11
- Device Tunnel tests don't work when run directly from Visual Studio due to bugs in windows which don't report device tunnel status correctly when profiles are generated outside of the SYSTEM context. As such these tests should be excluded from initial testing using the filter -trait:MachineTunnel
- To complete a successful test run use the script Run-TestsAsSYSTEM.ps1 to ensure that tests are operating in the SYSTEM context

# Roadmap

A couple of key development goals currently on the roadmap are:
- Re-release to enable existing and new DPC customers to continue to operate
- Improve build process using Github Actions
- Improve art assets
- Understand user requirements for additional features
