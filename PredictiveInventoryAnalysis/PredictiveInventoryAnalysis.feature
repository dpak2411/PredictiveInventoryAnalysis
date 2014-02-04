Feature: PredictiveInventoryAnalysis
	This app demonstrates the power of predictive analytics to optimize inventory analysis

Background:
  Given alteryx running at" http://gallery.alteryx.com/"
  And I am logged in using "deepak.manoharan@accionlabs.com" and "P@ssw0rd"

Scenario Outline: Run the predictive inventory analysis
When I run the application "<app>" on the store "<store>" and the category "<category>" and "<product>"
Then I see the report generated "<report>"
Examples: 
| app                                           | store     | category  | product      | report                        |
|"Merchandising - Predictive Inventory Analysis" | Arlington | Appliance | "Air Purifier" | "Inventory Analysis" |
