import {defaults,type Config} from './model.js';

type ConfigInput=Omit<Partial<Config>,'riskWeights'|'qualityAxes'>&{
  riskWeights?:Partial<Config['riskWeights']>&Record<string,number>;
  qualityAxes?:{
    maintainability?:Partial<Config['qualityAxes']['maintainability']>;
    testability?:Partial<Config['qualityAxes']['testability']>;
    architecture?:Partial<Config['qualityAxes']['architecture']>;
    knowledge?:unknown;
  };
};

export function mergeConfig(input:ConfigInput):Config{
  if(input.qualityAxes?.knowledge!==undefined)throw new Error('Unsupported configuration property: qualityAxes.knowledge');
  if(input.riskWeights&&'coverage' in input.riskWeights)throw new Error('Unsupported configuration property: riskWeights.coverage');
  return{...defaults,...input,untestedComplexity:{...defaults.untestedComplexity,...input.untestedComplexity},duplication:{...defaults.duplication,...input.duplication},qualityAxes:{maintainability:{...defaults.qualityAxes.maintainability,...input.qualityAxes?.maintainability,weights:{...defaults.qualityAxes.maintainability.weights,...input.qualityAxes?.maintainability?.weights}},testability:{...defaults.qualityAxes.testability,...input.qualityAxes?.testability,weights:{...defaults.qualityAxes.testability.weights,...input.qualityAxes?.testability?.weights}},architecture:{...defaults.qualityAxes.architecture,...input.qualityAxes?.architecture,weights:{...defaults.qualityAxes.architecture.weights,...input.qualityAxes?.architecture?.weights}}},overallRisk:{weights:{...defaults.overallRisk.weights,...input.overallRisk?.weights}},riskWeights:{...defaults.riskWeights,...input.riskWeights}};
}
