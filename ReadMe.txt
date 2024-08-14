

(See RequestApproval.cs)
1) Run this project to display two endpoints; RequestApproval_HttpStart and RequestApprovalEndpoint
2) Using Postman, post RequestApproval_HttpStart to return an 'id' which will be used as "InstanceId" in the below JSON.
3) Using Postman, post the below JSON in Body/Raw format with header "Content-Type" : "application/json"

{
    "InstanceId" : "473a1cf625724c268e3eda0ef178dddc",
    "IsApproved" : true
}

4) This action will call function 'PrintMessage' with content determined by the above JSON.


Human Interaction Design Pattern
(See ApprovalWorkflow.cs)

Approval from a manager might be required to sign off an invoice that exceeds a certain amount of money.
If the manager does not approve this invoice within the time limit, then the request is escalated.

The ApprovalWorkflow works similar to the above RequestApproval but is actioned on two events, 'ApprovalEvent' or a timeout event.
Failing to raise an 'ApprovalEvent' within the time limit, an escalation process kicks in.

In my mind this Azure Durable function is just a lightweight orchestration model, posting results to a message queue where other Azure functions are
awaiting to process the messages and say update a database.

REF: https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=in-process%2Cnodejs-v3%2Cv1-model&pivots=csharp
	