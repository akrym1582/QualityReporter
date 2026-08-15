import type {AxisScore,Config} from '../../model.js';import {axis} from '../quality-axis.js';
export const calculateTestabilityRisk=(components:{untestedComplexity?:number;lineCoverage?:number;branchCoverage?:number},config:Config['qualityAxes']['testability']):AxisScore=>config.enabled?axis(components,config.weights):axis({},{});
