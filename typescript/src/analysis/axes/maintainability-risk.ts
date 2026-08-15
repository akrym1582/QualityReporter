import type {AxisScore,Config} from '../../model.js';import {axis} from '../quality-axis.js';
export const calculateMaintainabilityRisk=(components:{complexity?:number;rework?:number;duplication?:number;issues?:number;methodSize?:number},config:Config['qualityAxes']['maintainability']):AxisScore=>config.enabled?axis(components,config.weights):axis({},{});
