export const calculateDuplicationRisk=(percentage:number,activityScore:number)=>percentage*(.60+.40*Math.min(100,Math.max(0,activityScore))/100);
