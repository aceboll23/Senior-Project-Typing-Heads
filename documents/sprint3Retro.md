# Sprint 3 Retrospective

## Quick Recap

The team conducted a Sprint 3 retrospective to discuss challenges and successes from the current sprint. Adler identified issues with the delete user functionality due to foreign key constraints and insufficient testing, while Ian and Logan discussed deployment problems related to Azure configuration and User Secrets. The team agreed that earlier deployments and better testing would help prevent similar issues in future sprints. Logan mentioned experiencing mental health challenges during the first week of the sprint, which impacted his productivity, while Ian noted that working closely with Logan went smoothly overall. The team discussed the importance of running thorough tests and making frequent pull requests to avoid merge conflicts, particularly given upcoming work during spring break.

---

## Next Steps

  - **Ian and Adler**
  - Work together to set up continuous deployment so that tests run and the app deploys automatically whenever code is merged into main, with early and final-sprint deployments for testing

- **All Team Members**
  - Submit frequent pull requests if working over spring break to avoid merge conflicts.
  - Aim to complete all development work by noon on the Sunday before the sprint deadline to allow for early merging and deployment testing.


---

## Summary

#### Sprint 3 User Functionality Issues
The team discussed issues from Sprint 3, particularly focusing on problems with deleting users and email functionality. Adler explained that the delete user feature failed because it cannot delete accounts with foreign keys attached, such as messages or sent emails. The email functionality also had issues due to lack of a proper sending service, making it only functional through Ethereal. Ian and Adler agreed to meet separately to address the API-related issues.

#### App Deployment Issues in Azure
The team discussed issues with a deployed app that worked locally but had problems in Azure, with Ian explaining they needed to use a specific name in Azure and would need to investigate further details. The group confirmed there would be 5 sprints in the next phase, with some discussion about whether one sprint was a fake sprint or focused on cleanup tasks. Ian concluded by noting that despite the deployment issues, the sprint was successful with all planned work completed.

### Logan's AI Project Discussion

Logan shared that the sprint went overall well but faced challenges with additional work from other classes and unexpected tasks. He mentioned creating a separate notification system and dealing with issues related to game groups and repository management, which caused some stress.

### Sprint Challenges and Management Issues

Logan shared that the sprint went overall well but faced challenges with additional work from other classes and unexpected tasks. He mentioned creating a separate notification system and dealing with issues related to game groups and repository management, which caused some stress.

### Sprint Performance and Improvement Plans

The team discussed sprint performance and identified areas for improvement, particularly around bug detection and deployment timing. Ian proposed completing all work by noon on Sundays to allow for early merging and bug fixes. The team agreed to postpone user story work due to uncertainty about Canvas requirements, with Ian expressing interest in working on the CCID feature instead. Logan mentioned an issue with not consistently running unit tests before implementation.

### Sprint 4 Unit Testing Discussion

Logan and Ian discussed their experiences with setting up unit tests for Sprint 4. Ian shared that his previous experience with bringing in games made attaching the API easier, though he initially overlooked the need to adjust tests for an additional entry point. Logan acknowledged that running tests thoroughly could be a potential issue, as neither of them had done so thoroughly in this case.

### Spring Break Code Management Planning

Adler and Ian agreed that unauthorized users should be directed to the login page rather than the sign-up page, as most users would be logging in rather than signing up. The team discussed managing code changes during spring break. Ian emphasized the importance of frequent pull request submissions to avoid merge conflicts, particularly since team members might be working on different parts of the codebase simultaneously.